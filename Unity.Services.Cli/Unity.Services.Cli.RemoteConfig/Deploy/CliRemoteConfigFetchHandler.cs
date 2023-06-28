using Unity.Services.RemoteConfig.Editor.Authoring.Core.Fetch;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class CliRemoteConfigFetchHandler : RemoteConfigFetchHandler
{
    public CliRemoteConfigFetchHandler(
        IRemoteConfigClient client,
        IFileSystem fileSystem,
        IJsonConverter jsonConverter,
        IRemoteConfigValidator remoteConfigValidator,
        IRemoteConfigParser remoteConfigParser) : base(client, fileSystem, jsonConverter, remoteConfigValidator, remoteConfigParser) { }

    protected override IRemoteConfigFile ConstructRemoteConfigFile(string path)
    {
        return new RemoteConfigFile(Path.GetFileName(path), path);
    }
}
