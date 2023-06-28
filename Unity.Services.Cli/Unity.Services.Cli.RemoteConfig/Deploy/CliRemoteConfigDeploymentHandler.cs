using Unity.Services.DeploymentApi.Editor;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Deployment;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class CliRemoteConfigDeploymentHandler : RemoteConfigDeploymentHandler
{
    public CliRemoteConfigDeploymentHandler(
        IRemoteConfigClient remoteConfigClient,
        IRemoteConfigParser remoteConfigParser,
        IRemoteConfigValidator remoteConfigValidator,
        IFormatValidator formatValidator,
        IConfigMerger configMerger,
        IJsonConverter jsonConverter,
        IFileSystem fileSystem) :
        base(remoteConfigClient, remoteConfigParser, remoteConfigValidator, formatValidator, configMerger, jsonConverter, fileSystem)
    {
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
