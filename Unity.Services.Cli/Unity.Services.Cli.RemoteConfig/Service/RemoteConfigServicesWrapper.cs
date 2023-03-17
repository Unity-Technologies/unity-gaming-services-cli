using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;

namespace Unity.Services.Cli.RemoteConfig.Service;

class RemoteConfigServicesWrapper : IRemoteConfigServicesWrapper
{
    public IRemoteConfigDeploymentHandler DeploymentHandler { get; }
    public ICliRemoteConfigClient RemoteConfigClient { get; }
    public IRemoteConfigService RemoteConfigService { get; }

    public IDeployFileService DeployFileService { get; }

    public ICliDeploymentOutputHandler DeploymentOutputHandler { get; }

    public IRemoteConfigScriptsLoader RemoteConfigScriptsLoader { get; }

    public RemoteConfigServicesWrapper(
        IRemoteConfigDeploymentHandler deploymentHandler,
        ICliRemoteConfigClient remoteConfigClient,
        ICliDeploymentOutputHandler deploymentOutputHandler,
        IDeployFileService deployFileService,
        IRemoteConfigService remoteConfigService,
        IRemoteConfigScriptsLoader remoteConfigScriptsLoader)
    {
        DeploymentHandler = deploymentHandler;
        RemoteConfigClient = remoteConfigClient;
        DeploymentOutputHandler = deploymentOutputHandler;
        DeployFileService = deployFileService;
        RemoteConfigService = remoteConfigService;
        RemoteConfigScriptsLoader = remoteConfigScriptsLoader;
    }
}
