using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.Deploy;

/// <summary>
/// Represents a schedule configuration file template.
/// </summary>
public class ScheduleConfigFile : IFileTemplate
{
    readonly IReadOnlyList<SchedulerEntry>? m_HelpConfigs;

    internal const string SchemaUrl =
        "https://ugs-config-schemas.unity3d.com/v1/schedules.schema.json";

    [JsonProperty("$schema")]
    public string Value { get; } = SchemaUrl;

    [JsonProperty("Configs")]
    [JsonConverter(typeof(ScheduleEntriesConverter))]
    public IReadOnlyList<SchedulerEntry> Entries { get; set; }

    [JsonIgnore]
    public string Extension => SchedulerConstants.DeployFileExtension;

    [JsonIgnore]
    public string FileBodyText => JsonConvert.SerializeObject(this, GetSerializationSettings());

    [JsonIgnore]
    public string HelpBodyText
    {
        get
        {
            if (m_HelpConfigs is not null)
            {
                return JsonConvert.SerializeObject(
                    new ScheduleConfigFile(m_HelpConfigs),
                    GetSerializationSettings());
            }

            var helpFile = new ScheduleConfigFile();
            helpFile.Entries.Single(entry => entry.Name == "Schedule1").StartAt = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            helpFile.Entries.Single(entry => entry.Name == "Schedule1").EndAt = new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            helpFile.Entries.Single(entry => entry.Name == "Schedule2").Schedule = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            return JsonConvert.SerializeObject(helpFile, GetSerializationSettings());
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfigFile"/> class with example schedules.
    /// </summary>
    public ScheduleConfigFile()
    {
        var startAt = DateTime.UtcNow.AddHours(1);
        var endAt = startAt.AddDays(1);
        Entries = new List<SchedulerEntry>
        {
            new()
            {
                Name = "Schedule1",
                EventName = "my.event.name",
                ScheduleType = "recurring",
                Schedule = "0 * * * *",
                PayloadVersion = 1,
                Payload = "{}",
                StartAt = startAt,
                EndAt = endAt
            },
            new()
            {
                Name = "Schedule2",
                EventName = "my.event.name",
                ScheduleType = "one-time",
                Schedule = DateTimeOffset.UtcNow.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                PayloadVersion = 1,
                Payload = "{ \"message\": \"Hello, world!\"}"
            },
            new()
            {
                Name = "Schedule3",
                EventName = "my.event.name",
                ScheduleType = "interval",
                Schedule = "3d",
                PayloadVersion = 1,
                Payload = "{}"
            }
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfigFile"/> class with the specified schedules.
    /// </summary>
    /// <param name="entries">The schedules to include in the configuration file.</param>
    [JsonConstructor]
    public ScheduleConfigFile(IReadOnlyList<SchedulerEntry> entries)
    {
        Entries = entries;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfigFile"/> class with separate runtime and help schedules.
    /// </summary>
    /// <param name="entries">The schedules to include in the generated configuration file.</param>
    /// <param name="helpConfigs">The deterministic schedules to include in help output.</param>
    public ScheduleConfigFile(
        IReadOnlyList<SchedulerEntry> entries,
        IReadOnlyList<SchedulerEntry> helpConfigs)
    {
        Entries = entries;
        m_HelpConfigs = helpConfigs;
    }

    public static JsonSerializerSettings GetSerializationSettings()
    {
        var settings = new JsonSerializerSettings()
        {
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Include,
            DateParseHandling = DateParseHandling.None,
        };
        return settings;
    }
}
