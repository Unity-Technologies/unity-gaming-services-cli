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
            return new DeploymentResult(m_DeploymentOutputHandler.Contents.ToList());
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
        IEnumerable<(string, string)> files,
        float progress = 0f,
        string status = "",
        string detail = "")
    {
        var contents = new List<DeployContent>();

        foreach (var file in files)
        {
            contents.Add(new DeployContent(file.Item1, "Remote Config", file.Item2, progress, status, detail));
        }

        return contents;
    }

    IReadOnlyCollection<DeployContent> ToDeployContents(
        IEnumerable<IRemoteConfigFile> files,
        float progress = 0f,
        string status = "",
        string detail = "")
    {
        var contents = new List<DeployContent>();

        foreach (var file in files)
        {
            foreach (var key in file.Content.entries.Keys)
            {
                contents.Add(new DeployContent(key, "Remote Config", file.Path, progress, status, detail));
            }
        }

        return contents;
    }
}
