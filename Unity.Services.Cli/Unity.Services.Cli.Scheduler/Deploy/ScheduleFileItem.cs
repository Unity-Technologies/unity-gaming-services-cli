using System.Runtime.Serialization;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Scheduler.Deploy;

[DataContract]
public class ScheduleFileItem : DeployContent
{
    public ScheduleConfigFile Content { get; }
    const string k_TriggersConfigFileType = "Schedule Config File";

    public ScheduleFileItem(ScheduleConfigFile content, string path, float progress = 0, DeploymentStatus? status = null)
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
}
