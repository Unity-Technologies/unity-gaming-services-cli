using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.Cli.CloudSave.Deploy;

[Serializable]
class SimpleResourceConfigFile : SimpleResource, IFileTemplate
{
    // TODO: replace your schema here
    [JsonProperty("$schema")]
    public string Schema => "https://ugs-config-schemas.unity3d.com/v1/my-service.schema.json";

    [JsonIgnore]
    public string Extension => Constants.SimpleFileExtension;

    [JsonIgnore]
    public string FileBodyText
    {
        get
        {
            var goodDefault = new SimpleResourceConfigFile()
            {
                AStrValue = "A Good Default",
                Name = "GoodDefaultName",
                NestedObj = new NestedObject
                {
                    NestedObjectBoolean = true,
                    NestedObjectString = "A Good Nested Default"
                }
            };
            return JsonConvert.SerializeObject(
                goodDefault,
                GetSerializationSettings());
        }
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
