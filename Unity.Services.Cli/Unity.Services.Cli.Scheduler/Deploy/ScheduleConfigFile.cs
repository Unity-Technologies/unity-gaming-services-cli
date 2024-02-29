using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.Deploy;

public class ScheduleConfigFile : IFileTemplate
{
    [JsonProperty("$schema")]
    public string Value { get; } = "https://ugs-config-schemas.unity3d.com/v1/schedules.schema.json";

    public IDictionary<string, ScheduleConfig> Configs { get; set; }

    [JsonIgnore]
    public string Extension => SchedulerConstants.DeployFileExtension;

    [JsonIgnore]
    public string FileBodyText => JsonConvert.SerializeObject(this, GetSerializationSettings());

    public ScheduleConfigFile()
    {
        Configs = new Dictionary<string, ScheduleConfig>()
        {
            {
                "Schedule1",
                new ("Schedule1",
                    "EventType1",
                    "recurring",
                    "0 * * * *",
                    1,
                    "{}")

            },
            {
                "Schedule2",
                new ("Schedule2",
                    "EventType2",
                    "one-time",
                    DateTime.Now.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    1,
                    "{ \"message\": \"Hello, world!\"}")
            },
        };
    }

    [JsonConstructor]
    public ScheduleConfigFile(IDictionary<string, ScheduleConfig> configs)
    {
        Configs = configs;
    }

    public static JsonSerializerSettings GetSerializationSettings()
    {
        var settings = new JsonSerializerSettings()
        {
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,

        };
        return settings;
    }
}
