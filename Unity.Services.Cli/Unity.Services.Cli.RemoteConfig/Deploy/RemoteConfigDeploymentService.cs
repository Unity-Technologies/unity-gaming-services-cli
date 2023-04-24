using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Results;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

internal class RemoteConfigDeploymentService : IDeploymentService
{
    string m_ServiceType;
    string m_DeployFileExtension;
    IRemoteConfigDeploymentHandler m_DeploymentHandler;
    ICliRemoteConfigClient m_RemoteConfigClient;
    ICliDeploymentOutputHandler m_DeploymentOutputHandler;
    IRemoteConfigScriptsLoader m_RemoteConfigScriptsLoader;

    public RemoteConfigDeploymentService(
        IRemoteConfigServicesWrapper servicesWrapper
    )
    {
        m_DeploymentHandler = servicesWrapper.DeploymentHandler;
        m_RemoteConfigClient = servicesWrapper.RemoteConfigClient;
        m_DeploymentOutputHandler = servicesWrapper.DeploymentOutputHandler;
        m_RemoteConfigScriptsLoader = servicesWrapper.RemoteConfigScriptsLoader;
        m_ServiceType = "Remote Config";
        m_DeployFileExtension = ".rc";
    }

    string IDeploymentService.ServiceType => m_ServiceType;

    string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

    public async Task<DeploymentResult> Deploy(DeployInput deployInput, IReadOnlyList<string> filePaths, string projectId, string environmentId,
        StatusContext? loadingContext, CancellationToken cancellationToken)
    {
        DeployResult deploymentResult = null!;
        m_RemoteConfigClient.Initialize(projectId, environmentId, cancellationToken);
        var loadResult = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filePaths, m_DeploymentOutputHandler.Contents);
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

        if (deploymentResult == null || configFiles == null)
        {
            return new DeploymentResult(m_DeploymentOutputHandler.Contents.GetUniqueDescriptions().ToList());
        }

        var failed = ToDeployContents(deploymentResult.Failed).Concat(loadResult.Failed).ToList();

        return new DeploymentResult(
            ToDeployContents(deploymentResult.Created),
            ToDeployContents(deploymentResult.Updated),
            ToDeployContents(deploymentResult.Deleted),
            ToDeployContents(deploymentResult.Deployed, 100, "Deployed", "Deployed Successfully"),
            failed);
    }

    IReadOnlyCollection<DeployContent> ToDeployContents(
        IReadOnlyCollection<RemoteConfigEntry> entries,
        float progress = 0f,
        string status = "",
        string detail = "")
    {
        return entries
            .Select(entry =>
            {
                var filePath = entry.File == null ? "Remote" : entry.File.Path;
                return new DeployContent(entry.Key, "Remote Config", filePath, progress, status, detail);
            })
            .ToList();
    }

    IReadOnlyCollection<DeployContent> ToDeployContents(
        IReadOnlyList<IRemoteConfigFile> files,
        float progress = 0f,
        string status = "",
        string detail = "")
    {

        var entries = files
            .Where(file => file.Entries != null)
            .SelectMany(file => file.Entries)
            .ToList();

        return entries
            .Select(entry => new DeployContent(entry.Key, "Remote Config", entry.File.Path, progress, status, detail))
            .ToList();
    }
}
