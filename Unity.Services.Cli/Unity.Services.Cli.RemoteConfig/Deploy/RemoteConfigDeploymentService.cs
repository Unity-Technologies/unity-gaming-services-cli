using Spectre.Console;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.Cli.RemoteConfig.Exceptions;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;

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
        m_RemoteConfigClient.Initialize(projectId, environmentId, cancellationToken);
        var configFiles = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(filePaths, m_DeploymentOutputHandler.Contents);
        loadingContext?.Status($"Deploying {m_ServiceType} Files...");
        try
        {
            var reconcile = false;
            var dryRun = false;

            await m_DeploymentHandler.DeployAsync(configFiles, reconcile, dryRun);
        }
        catch (RemoteConfigDeploymentException)
        {
            // Ignoring it because this exception should already be logged into the deployment content status
        }
        catch (ApiException)
        {
            // Ignoring it because this exception should already be logged into the deployment content status
        }

        return new DeploymentResult(m_DeploymentOutputHandler.Contents.ToList());
    }
}
