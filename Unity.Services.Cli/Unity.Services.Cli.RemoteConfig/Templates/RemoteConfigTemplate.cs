using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using JsonConverter = Unity.Services.Cli.RemoteConfig.Deploy.JsonConverter;

namespace Unity.Services.Cli.RemoteConfig.Templates;

class RemoteConfigTemplate : RemoteConfigFileContent , IFileTemplate
{
    [JsonProperty("$schema")]
    public string Value = "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json";

    public RemoteConfigTemplate() {
        entries = new Dictionary<string, object>()
        {
            { "string_key", "string_value" },
            { "int_key", 1 },
            { "bool_key", true },
            { "long_key", 10000 },
            { "float_key", 1 },
            { "json_key", JObject.Parse("{'sample_key': 'sample_value'}") }
        };
        types =  new Dictionary<string, ConfigType>()
        {
            { "long_key", ConfigType.LONG },
            { "float_key", ConfigType.FLOAT }
        };
    }

    [JsonIgnore]
    public string Extension => ".rc";
    [JsonIgnore]
    public string FileBodyText => new JsonConverter().SerializeObject(this);
}
