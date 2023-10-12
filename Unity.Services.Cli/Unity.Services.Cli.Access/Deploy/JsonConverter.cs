using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Unity.Services.Access.Authoring.Core.Json;

namespace Unity.Services.Cli.Access.Deploy;

public class JsonConverter : IJsonConverter
{
    static JsonSerializerSettings GetSettings(bool matchCamelCaseFieldName = false)
    {
        var contractResolver = matchCamelCaseFieldName ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver();

        return new JsonSerializerSettings()
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
    }

    public T DeserializeObject<T>(string value, bool matchCamelCaseFieldName = false)
    {
        var settings = GetSettings(matchCamelCaseFieldName);
        return JsonConvert.DeserializeObject<T>(value, settings)!;
    }

    public string SerializeObject<T>(T obj)
    {
        var settings = GetSettings();
        return JsonConvert.SerializeObject(obj, settings);
    }
}
