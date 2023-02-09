using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class JsonConverter : IJsonConverter
{
    public T DeserializeObject<T>(string value, bool matchCamelCaseFieldName = false)
    {
        var contractResolver = matchCamelCaseFieldName ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver();

        var settings = new JsonSerializerSettings()
        {
            ContractResolver = contractResolver
        };

        return JsonConvert.DeserializeObject<T>(value, settings)!;
    }

    public string SerializeObject<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented, new StringEnumConverter());
    }
}
