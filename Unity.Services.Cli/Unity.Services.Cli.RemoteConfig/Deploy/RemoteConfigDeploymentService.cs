using Spectre.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.ErrorHandling;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

internal class RemoteConfigDeploymentService : IDeploymentService
{
    IUnityEnvironment m_UnityEnvironment;
    string m_ServiceType;
    string m_DeployFileExtension;
    IRemoteConfigDeploymentHandler m_DeploymentHandler;
    ICliRemoteConfigClient m_RemoteConfigClient;
    IDeployFileService m_DeployFileService;
    ICliDeploymentOutputHandler m_DeploymentOutputHandler;
    IRemoteConfigScriptsLoader m_RemoteConfigScriptsLoader;

    public RemoteConfigDeploymentService(
        IUnityEnvironment unityEnvironment,
        IRemoteConfigServicesWrapper servicesWrapper
    )
    {
        m_UnityEnvironment = unityEnvironment;
        m_DeploymentHandler = servicesWrapper.DeploymentHandler;
        m_RemoteConfigClient = servicesWrapper.RemoteConfigClient;
        m_DeployFileService = servicesWrapper.DeployFileService;
        m_DeploymentOutputHandler = servicesWrapper.DeploymentOutputHandler;
        m_RemoteConfigScriptsLoader = servicesWrapper.RemoteConfigScriptsLoader;
        m_ServiceType = "Remote Config";
        m_DeployFileExtension = ".rc";
    }

    string IDeploymentService.ServiceType => m_ServiceType;

    string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

    public async Task<DeploymentResult> Deploy(DeployInput input, StatusContext? loadingContext, CancellationToken cancellationToken)
    {
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync();
        m_RemoteConfigClient.Initialize(input.CloudProjectId!, environmentId, cancellationToken);
        var remoteConfigFiles = m_DeployFileService.ListFilesToDeploy(input.Paths, ".rc").ToList();
        var configFiles = await m_RemoteConfigScriptsLoader.LoadScriptsAsync(remoteConfigFiles, m_DeploymentOutputHandler.Contents);
        loadingContext?.Status($"Deploying {m_ServiceType} Files...");
        try
        {
            bool reconcile = false;
            await m_DeploymentHandler.DeployAsync(configFiles, reconcile);
        }
        catch (RemoteConfigDeploymentException)
        {
            /*
             * Ignoring this because we already catch exceptions from UpdateScriptStatus() for each script and we don't
             * want to stop execution when a script generates an exception.
             */
        }

        return new DeploymentResult(m_DeploymentOutputHandler.Contents.ToList());
    }
}
