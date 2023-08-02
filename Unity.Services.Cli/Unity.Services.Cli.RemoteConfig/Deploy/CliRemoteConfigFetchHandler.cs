using Unity.Services.DeploymentApi.Editor;
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

    protected override void UpdateStatus(
        IRemoteConfigFile remoteConfigFile,
        string status,
        string detail,
        SeverityLevel severityLevel)
    {
        var file = (RemoteConfigFile)remoteConfigFile;
        file.Status = new DeploymentStatus(status, detail, severityLevel);
    }

    protected override void UpdateProgress(IRemoteConfigFile remoteConfigFile, float progress)
    {
        var file = (RemoteConfigFile)remoteConfigFile;
        file.Progress = progress;
    }
}
