using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Networking;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class CliRemoteConfigDeploymentHandler : RemoteConfigDeploymentHandler, ICliDeploymentOutputHandler
{
    public ICollection<DeployContent> Contents { get; } = new List<DeployContent>();

    public CliRemoteConfigDeploymentHandler(
        IRemoteConfigClient remoteConfigClient,
        IRemoteConfigParser remoteConfigParser,
        IRemoteConfigValidator remoteConfigValidator,
        IFormatValidator formatValidator,
        IConfigMerger configMerger,
        IJsonConverter jsonConverter,
        IFileReader fileReader) :
        base(remoteConfigClient, remoteConfigParser, remoteConfigValidator, formatValidator, configMerger, jsonConverter, fileReader)
    {
    }

    protected override void UpdateStatus(IRemoteConfigFile remoteConfigFile, string status, string detail,
        StatusSeverityLevel severityLevel)
    {
        var content = Contents.First(c => string.Equals(remoteConfigFile.Path, c.Path));
        content.Status = status;
        content.Detail = detail;
    }

    protected override void UpdateProgress(IRemoteConfigFile remoteConfigFile, float progress)
    {
        var deployContent = Contents.First(c => string.Equals(c.Path, remoteConfigFile.Path));
        deployContent.Progress = progress;
    }
}
