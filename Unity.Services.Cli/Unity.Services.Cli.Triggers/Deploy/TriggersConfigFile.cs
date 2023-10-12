using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Cli.Triggers.Deploy;

public class TriggersConfigFile : IFileTemplate
{
    public IList<TriggerConfig> Configs { get; set; }

    [JsonIgnore]
    public string Extension => TriggersConstants.DeployFileExtension;

    [JsonIgnore]
    public string FileBodyText => JsonConvert.SerializeObject(this, GetSerializationSettings());

    public TriggersConfigFile()
    {
        Configs = new List<TriggerConfig>()
        {
            new ("Trigger 1", "EventType1", "cloud-code", "urn:ugs:cloud-code:MyScript"),
            new ("Trigger 2", "EventType2", "cloud-code", "urn:ugs:cloud-code:MyModule/MyFunction")
        };
    }

    [JsonConstructor]
    public TriggersConfigFile(IList<TriggerConfig> configs)
    {
        Configs = configs;
    }

    public static JsonSerializerSettings GetSerializationSettings()
    {
        var settings = new JsonSerializerSettings()
        {
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
        return settings;
    }
}
