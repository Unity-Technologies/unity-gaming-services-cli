using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

interface ICliRemoteConfigClient : IRemoteConfigClient
{
    void Initialize(string projectId, string environmentId, CancellationToken cancellationToken);
}
