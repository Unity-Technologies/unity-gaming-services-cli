using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Cli.Triggers.Deploy;

public class TriggersConfigFile : IFileTemplate
{
    [JsonProperty("$schema")]
    public string Value => "https://ugs-config-schemas.unity3d.com/v1/triggers.schema.json";

    public IList<TriggerConfig> Configs { get; set; }

    [JsonIgnore]
    public string Extension => TriggersConstants.DeployFileExtension;

    [JsonIgnore]
    public string FileBodyText => JsonConvert.SerializeObject(this, GetSerializationSettings());

    public TriggersConfigFile()
    {
        Configs = new List<TriggerConfig>()
        {
            new ("Trigger 1", "EventType1", "cloud-code", "urn:ugs:cloud-code:MyScript", ""),
            new ("Trigger 2", "EventType2", "cloud-code", "urn:ugs:cloud-code:MyModule/MyFunction", ""),
            new ("Trigger 3", "EventType3", "cloud-code", "urn:ugs:cloud-code:MyModule/MyFunction", "data['value'] > 5")
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
