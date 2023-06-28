using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Results;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

partial class RemoteConfigDeploymentService : IDeploymentService
{
    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly string m_DeployFileExtension;
    readonly IRemoteConfigDeploymentHandler m_DeploymentHandler;
    readonly ICliRemoteConfigClient m_RemoteConfigClient;
    readonly IRemoteConfigScriptsLoader m_RemoteConfigScriptsLoader;

    public RemoteConfigDeploymentService(
        IRemoteConfigServicesWrapper servicesWrapper
    )
    {
        m_DeploymentHandler = servicesWrapper.DeploymentHandler;
        m_RemoteConfigClient = servicesWrapper.RemoteConfigClient;
        m_RemoteConfigScriptsLoader = servicesWrapper.RemoteConfigScriptsLoader;
        m_ServiceType = "Remote Config";
        m_ServiceName = "remote-config";
        m_DeployFileExtension = ".rc";
    }

    string IDeploymentService.ServiceType => m_ServiceType;

    string IDeploymentService.ServiceName => m_ServiceName;

    string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        DeployResult deploymentResult = null!;
        m_RemoteConfigClient.Initialize(projectId, environmentId, cancellationToken);
        var loadResult = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filePaths, cancellationToken);
        var configFiles = loadResult.Loaded.ToList();
        loadingContext?.Status($"Deploying {m_ServiceType} Files...");

        try
        {
            var reconcile = deployInput.Reconcile;
            var dryRun = deployInput.DryRun;

            deploymentResult = await m_DeploymentHandler.DeployAsync(configFiles, reconcile, dryRun);
        }
        catch (RemoteConfigDeploymentException)
        {
            // Ignoring it because this exception should already be logged into the deployment content status
        }
        catch (ApiException)
        {
            // Ignoring it because this exception should already be logged into the deployment content status
        }

        if (deploymentResult == null)
        {
            return new DeploymentResult(loadResult.Loaded.Concat(loadResult.Failed).ToList());
        }

        var failed = deploymentResult
            .Failed
            .Select(d => (RemoteConfigFile)d)
            .UnionBy(loadResult.Failed, f => f.Path)
            .ToList();

        return new DeploymentResult(
            ToDeployContents(deploymentResult.Updated, 100, "Updated"),
            ToDeployContents(deploymentResult.Deleted, 100, "Deleted"),
            ToDeployContents(deploymentResult.Created, 100, "Created"),
            deploymentResult.Deployed.Select(d => (RemoteConfigFile)d).ToList(),
            failed,
            deployInput.DryRun);
    }

    static IReadOnlyList<IDeploymentItem> ToDeployContents(
        IReadOnlyCollection<RemoteConfigEntry> entries,
        float progress = 0f,
        string status = "",
        string detail = "")
    {
        return entries
            .Select(entry =>
            {
                var filePath = entry.File == null ? "Remote" : entry.File.Path;
                return new CliRemoteConfigEntry(entry.Key, "RemoteConfig Entry", filePath, progress, status, detail);
            })
            .ToList();
    }
}
