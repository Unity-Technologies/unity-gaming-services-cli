using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.Deploy;

class SchedulerResourceLoader : ISchedulerResourceLoader
{
    const string k_ConfigsProperty = "Configs";
    const string k_SchemaProperty = "$schema";
    static readonly ConcurrentDictionary<string, SemaphoreSlim> k_FileLocks = new(StringComparer.Ordinal);
    readonly IFileSystem m_FileSystem;

    public SchedulerResourceLoader(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;
    }

    public async Task<List<SchedulerEntryDeploymentItem>> ReadResource(string path, CancellationToken cancellationToken)
    {
        var deployableItems = new List<SchedulerEntryDeploymentItem>();
        try
        {
            var content = await m_FileSystem.ReadAllText(path, cancellationToken);
            var scheduleConfigFile = JsonConvert.DeserializeObject<ScheduleConfigFile>(
                content,
                ScheduleConfigFile.GetSerializationSettings())
                ?? throw new JsonSerializationException($"Schedule file '{path}' must be a JSON object.");

            deployableItems.AddRange(scheduleConfigFile.Entries.Select(entry =>
                new SchedulerEntryDeploymentItem(path)
                {
                    entry = entry,
                    Name = entry.Name
                }));

            return deployableItems;
        }
        catch (Exception ex)
        {
            var failEntry = new SchedulerEntryDeploymentItem(path)
            {
                Status = new DeploymentStatus(
                    "Failed to Load",
                    $"Error reading file: {ex.Message}",
                    SeverityLevel.Error)
            };

            deployableItems.Add(failEntry);
            return deployableItems;
        }
    }

    public async Task CreateOrUpdateResource(SchedulerEntryDeploymentItem deployableItem, CancellationToken token)
    {
        var path = Path.GetFullPath(deployableItem.Path);
        var fileLock = k_FileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync(token);
        try
        {
            var document = await ReadDocumentOrCreate(path, token);
            var configs = GetConfigs(document, path);
            var name = GetEntryName(deployableItem);
            var serializer = JsonSerializer.Create(ScheduleConfigFile.GetSerializationSettings());
            var entry = JObject.FromObject(deployableItem.entry, serializer);
            entry.Remove(nameof(SchedulerEntry.Name));
            entry.Remove(nameof(SchedulerEntry.Id));
            configs[name] = entry;

            await m_FileSystem.WriteAllText(
                path,
                document.ToString(Formatting.Indented),
                token);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task DeleteResource(SchedulerEntryDeploymentItem deploymentItem, CancellationToken token)
    {
        var path = Path.GetFullPath(deploymentItem.Path);
        var fileLock = k_FileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync(token);
        try
        {
            JObject document;
            try
            {
                document = await ReadDocument(path, token);
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            var configs = GetConfigs(document, path);
            if (!configs.Remove(GetEntryName(deploymentItem)))
                return;

            if (!configs.Properties().Any())
            {
                await m_FileSystem.Delete(path, token);
                return;
            }

            await m_FileSystem.WriteAllText(
                path,
                document.ToString(Formatting.Indented),
                token);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public void DeserializeAndPopulateFromPath(SchedulerEntryDeploymentItem config, string path)
    {
        throw new NotImplementedException();
    }

    async Task<JObject> ReadDocumentOrCreate(string path, CancellationToken token)
    {
        try
        {
            return await ReadDocument(path, token);
        }
        catch (FileNotFoundException)
        {
            return CreateDocument();
        }
        catch (DirectoryNotFoundException)
        {
            return CreateDocument();
        }
    }

    async Task<JObject> ReadDocument(string path, CancellationToken token)
    {
        var content = await m_FileSystem.ReadAllText(path, token);
        return JObject.Parse(content);
    }

    static JObject CreateDocument()
    {
        return new JObject
        {
            [k_SchemaProperty] = ScheduleConfigFile.SchemaUrl,
            [k_ConfigsProperty] = new JObject()
        };
    }

    static JObject GetConfigs(JObject document, string path)
    {
        if (document[k_ConfigsProperty] is JObject configs)
        {
            return configs;
        }

        throw new JsonSerializationException($"'{k_ConfigsProperty}' in '{path}' must be a JSON object.");
    }

    static string GetEntryName(SchedulerEntryDeploymentItem item)
    {
        var name = item.entry?.Name ?? item.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new JsonSerializationException($"A schedule in '{item.Path}' does not have a name.");
        }

        return name;
    }
}
