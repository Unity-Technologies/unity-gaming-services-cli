using Newtonsoft.Json;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.IO;
using Unity.Services.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.Deploy;

public class ScheduleResourceLoader : IScheduleResourceLoader
{
    readonly IFileSystem m_FileSystem;

    public ScheduleResourceLoader(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;
    }

    public async Task<ScheduleFileItem> LoadResource(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var content = await m_FileSystem.ReadAllText(filePath, cancellationToken);
            var triggersConfigFile = JsonConvert.DeserializeObject<ScheduleConfigFile>(
                content,
                ScheduleConfigFile.GetSerializationSettings())!;

            foreach (var config in triggersConfigFile.Configs)
            {
                config.Value.Path = filePath;
                config.Value.Name = config.Key;
            }
            return new ScheduleFileItem(triggersConfigFile, filePath);
        }
        catch (Exception ex)
        {
            var res = new ScheduleFileItem(new ScheduleConfigFile(new Dictionary<string, ScheduleConfig>()), filePath)
            {
                Status = new DeploymentStatus(
                    "Failed to Load",
                    $"Error reading file: {ex.Message}",
                    SeverityLevel.Error)
            };
            return res;
        }
    }
}
