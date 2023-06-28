using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;

namespace Unity.Services.Cli.RemoteConfig.Service;

interface IRemoteConfigServicesWrapper
{
    IRemoteConfigDeploymentHandler DeploymentHandler { get; }

    ICliRemoteConfigClient RemoteConfigClient { get; }

    IRemoteConfigScriptsLoader RemoteConfigScriptsLoader { get; }

}
