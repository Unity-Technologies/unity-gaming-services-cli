using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;

namespace Unity.Services.Cli.RemoteConfig.Service;

class RemoteConfigServicesWrapper : IRemoteConfigServicesWrapper
{
    public IRemoteConfigDeploymentHandler DeploymentHandler { get; }
    public ICliRemoteConfigClient RemoteConfigClient { get; }
    public IRemoteConfigService RemoteConfigService { get; }

    public IRemoteConfigScriptsLoader RemoteConfigScriptsLoader { get; }

    public RemoteConfigServicesWrapper(
        IRemoteConfigDeploymentHandler deploymentHandler,
        ICliRemoteConfigClient remoteConfigClient,
        IRemoteConfigService remoteConfigService,
        IRemoteConfigScriptsLoader remoteConfigScriptsLoader)
    {
        DeploymentHandler = deploymentHandler;
        RemoteConfigClient = remoteConfigClient;
        RemoteConfigService = remoteConfigService;
        RemoteConfigScriptsLoader = remoteConfigScriptsLoader;
    }
}
