using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class ScheduleEntriesConverterTests
{
    [Test]
    public void Deserialize_SetsNameFromConfigKeyWithoutCreatingId()
    {
        const string json = """
            {
              "Configs": {
                "Schedule1": {
                  "EventName": "example.event",
                  "Type": "recurring",
                  "Schedule": "0 * * * *",
                  "PayloadVersion": 1,
                  "Payload": "{}"
                }
              }
            }
            """;

        var file = JsonConvert.DeserializeObject<ScheduleConfigFile>(
            json,
            ScheduleConfigFile.GetSerializationSettings());

        Assert.Multiple(() =>
        {
            Assert.That(file, Is.Not.Null);
            Assert.That(file!.Entries, Has.Count.EqualTo(1));
            Assert.That(file.Entries[0].Name, Is.EqualTo("Schedule1"));
            Assert.That(file.Entries[0].Id, Is.Null);
        });
    }

    [Test]
    public void Deserialize_PreservesDuplicateConfigNames()
    {
        const string json = """
            {
              "Configs": {
                "Schedule1": {
                  "EventName": "first.event"
                },
                "Schedule1": {
                  "EventName": "second.event"
                }
              }
            }
            """;

        var file = JsonConvert.DeserializeObject<ScheduleConfigFile>(
            json,
            ScheduleConfigFile.GetSerializationSettings());

        Assert.Multiple(() =>
        {
            Assert.That(file, Is.Not.Null);
            Assert.That(file!.Entries.Select(entry => entry.Name),
                Is.EqualTo(new[] { "Schedule1", "Schedule1" }));
            Assert.That(file.Entries.Select(entry => entry.EventName),
                Is.EqualTo(new[] { "first.event", "second.event" }));
        });
    }

    [Test]
    public void Serialize_UsesNameAsConfigKeyAndOmitsNameAndId()
    {
        var file = new ScheduleConfigFile(new List<SchedulerEntry>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Schedule1",
                EventName = "example.event",
                ScheduleType = "recurring",
                Schedule = "0 * * * *",
                PayloadVersion = 1,
                Payload = "{}"
            }
        });

        var document = JObject.Parse(JsonConvert.SerializeObject(
            file,
            ScheduleConfigFile.GetSerializationSettings()));
        var schedule = document["Configs"]!["Schedule1"]!;

        Assert.Multiple(() =>
        {
            Assert.That(schedule["EventName"]!.Value<string>(), Is.EqualTo("example.event"));
            Assert.That(schedule[nameof(SchedulerEntry.Name)], Is.Null);
            Assert.That(schedule[nameof(SchedulerEntry.Id)], Is.Null);
        });
    }

    [Test]
    public void Serialize_RejectsDuplicateNames()
    {
        var file = new ScheduleConfigFile(new List<SchedulerEntry>
        {
            new() { Name = "Schedule1" },
            new() { Name = "Schedule1" }
        });

        Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(
            file,
            ScheduleConfigFile.GetSerializationSettings()));
    }
}
