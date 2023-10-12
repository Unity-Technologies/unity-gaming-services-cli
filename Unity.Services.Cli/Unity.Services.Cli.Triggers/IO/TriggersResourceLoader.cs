using Newtonsoft.Json;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.IO;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Cli.Triggers.IO;

 class TriggersResourceLoader : ITriggersResourceLoader
{
    readonly IFileSystem m_FileSystem;

    public TriggersResourceLoader(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;
    }

    public async Task<TriggersFileItem> LoadResource(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var content = await m_FileSystem.ReadAllText(filePath, cancellationToken);
            var triggersConfigFile = JsonConvert.DeserializeObject<TriggersConfigFile>(
                content,
                TriggersConfigFile.GetSerializationSettings())!;

            foreach (var config in triggersConfigFile.Configs)
            {
                config.Path = filePath;
            }
            return new TriggersFileItem(triggersConfigFile, filePath);
        }
        catch (Exception ex)
        {
            var res = new TriggersFileItem(new TriggersConfigFile(new List<TriggerConfig>()) , filePath);
            res.Status = new DeploymentStatus(
                "Failed to Load",
                $"Error reading file: {ex.Message}",
                SeverityLevel.Error);
            return res;
        }
    }
}
