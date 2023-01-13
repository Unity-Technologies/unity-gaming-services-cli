using Unity.Services.RemoteConfig.Editor.Authoring.Core.Networking;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

interface ICliRemoteConfigClient : IRemoteConfigClient
{
    void Initialize(string projectId, string environmentId, CancellationToken cancellationToken);
}
