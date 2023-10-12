using System.Runtime.Serialization;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Triggers.Deploy;

[DataContract]
public class TriggersFileItem : DeployContent
{
    const string k_TriggersConfigFileType = "Triggers Config File";

    public TriggersFileItem(TriggersConfigFile content, string path, float progress = 0, DeploymentStatus? status = null)
        : base(
            System.IO.Path.GetFileName(path),
            k_TriggersConfigFileType,
            path,
            progress,
            status)
    {
        Content = content;
        Path = path;
    }

    public TriggersConfigFile Content { get; }
}
