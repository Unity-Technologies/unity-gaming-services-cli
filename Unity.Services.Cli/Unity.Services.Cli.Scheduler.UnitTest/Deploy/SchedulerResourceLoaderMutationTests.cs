using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class SchedulerResourceLoaderMutationTests
{
    const string k_Path = "/project/schedules.sched";
    InMemoryFileSystem m_FileSystem = null!;
    SchedulerResourceLoader m_Loader = null!;

    [SetUp]
    public void SetUp()
    {
        m_FileSystem = new InMemoryFileSystem();
        m_Loader = new SchedulerResourceLoader(m_FileSystem);
    }

    [Test]
    public async Task CreateOrUpdateResource_UpdatesOnlyTargetEntry()
    {
        m_FileSystem.Set(k_Path, """
            {
              "$schema": "schema",
              "Custom": true,
              "Configs": {
                "Schedule1": {
                  "EventName": "old.event",
                  "Unknown": "preserve"
                },
                "Schedule2": {
                  "EventName": "other.event",
                  "Unknown": "preserve"
                }
              }
            }
            """);

        await m_Loader.CreateOrUpdateResource(
            CreateItem("Schedule1", k_Path, "new.event"),
            CancellationToken.None);

        var document = m_FileSystem.Parse(k_Path);
        Assert.Multiple(() =>
        {
            Assert.That((bool?)document["Custom"], Is.True);
            Assert.That((string?)document["Configs"]!["Schedule1"]!["EventName"], Is.EqualTo("new.event"));
            Assert.That(document["Configs"]!["Schedule1"]!["Unknown"], Is.Null);
            Assert.That((string?)document["Configs"]!["Schedule2"]!["Unknown"], Is.EqualTo("preserve"));
            Assert.That(document["Configs"]!["Schedule1"]!["Name"], Is.Null);
        });
    }

    [Test]
    public async Task CreateOrUpdateResource_CreatesMissingFileAndEntry()
    {
        await m_Loader.CreateOrUpdateResource(
            CreateItem("Schedule1", k_Path, "event.name"),
            CancellationToken.None);

        var document = m_FileSystem.Parse(k_Path);
        Assert.Multiple(() =>
        {
            Assert.That((string?)document["$schema"], Is.EqualTo("https://ugs-config-schemas.unity3d.com/v1/schedules.schema.json"));
            Assert.That((string?)document["Configs"]!["Schedule1"]!["EventName"], Is.EqualTo("event.name"));
            Assert.That(document["Configs"]!.Children<JProperty>().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DeleteResource_RemovesOnlyTargetEntry()
    {
        m_FileSystem.Set(k_Path, CreateFile("Schedule1", "Schedule2"));

        await m_Loader.DeleteResource(
            CreateItem("Schedule1", k_Path, "event.name"),
            CancellationToken.None);

        var configs = m_FileSystem.Parse(k_Path)["Configs"]!;
        Assert.Multiple(() =>
        {
            Assert.That(configs["Schedule1"], Is.Null);
            Assert.That(configs["Schedule2"], Is.Not.Null);
            Assert.That(m_FileSystem.Exists(k_Path), Is.True);
        });
    }

    [Test]
    public async Task DeleteResource_DeletesFileWhenRemovingLastEntry()
    {
        m_FileSystem.Set(k_Path, CreateFile("Schedule1"));

        await m_Loader.DeleteResource(
            CreateItem("Schedule1", k_Path, "event.name"),
            CancellationToken.None);

        Assert.That(m_FileSystem.Exists(k_Path), Is.False);
    }

    [Test]
    public async Task CreateOrUpdateResource_ConcurrentLoadersPreserveBothEntries()
    {
        var secondLoader = new SchedulerResourceLoader(m_FileSystem);

        await Task.WhenAll(
            m_Loader.CreateOrUpdateResource(
                CreateItem("Schedule1", k_Path, "event.one"),
                CancellationToken.None),
            secondLoader.CreateOrUpdateResource(
                CreateItem("Schedule2", k_Path, "event.two"),
                CancellationToken.None));

        var configs = m_FileSystem.Parse(k_Path)["Configs"]!;
        Assert.Multiple(() =>
        {
            Assert.That((string?)configs["Schedule1"]!["EventName"], Is.EqualTo("event.one"));
            Assert.That((string?)configs["Schedule2"]!["EventName"], Is.EqualTo("event.two"));
        });
    }

    static SchedulerEntryDeploymentItem CreateItem(string name, string path, string eventName)
    {
        return new SchedulerEntryDeploymentItem(path)
        {
            Name = name,
            entry = new SchedulerEntry
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                EventName = eventName,
                ScheduleType = "recurring",
                Schedule = "0 * * * *",
                PayloadVersion = 1,
                Payload = "{}"
            }
        };
    }

    static string CreateFile(params string[] names)
    {
        var configs = new JObject();
        foreach (var name in names)
        {
            configs[name] = new JObject
            {
                ["EventName"] = $"event.{name}"
            };
        }

        return new JObject
        {
            ["Configs"] = configs
        }.ToString();
    }

    sealed class InMemoryFileSystem : IFileSystem
    {
        readonly ConcurrentDictionary<string, string> m_Files = new(StringComparer.Ordinal);

        public Task<string> ReadAllText(string path, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!m_Files.TryGetValue(Normalize(path), out var contents))
                throw new FileNotFoundException(null, path);

            return Task.FromResult(contents);
        }

        public Task WriteAllText(string path, string contents, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            m_Files[Normalize(path)] = contents;
            return Task.CompletedTask;
        }

        public Task Delete(string path, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            m_Files.TryRemove(Normalize(path), out _);
            return Task.CompletedTask;
        }

        public void Set(string path, string contents)
        {
            m_Files[Normalize(path)] = contents;
        }

        public bool Exists(string path)
        {
            return m_Files.ContainsKey(Normalize(path));
        }

        public JObject Parse(string path)
        {
            return JObject.Parse(m_Files[Normalize(path)]);
        }

        static string Normalize(string path)
        {
            return Path.GetFullPath(path);
        }
    }
}
