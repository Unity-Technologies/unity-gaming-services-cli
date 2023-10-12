using Newtonsoft.Json;

namespace Unity.Services.Cli.Economy.Authoring.IO;

class EconomyJsonConverter : IEconomyJsonConverter
{
    public T? DeserializeObject<T>(string value)
    {
        return JsonConvert.DeserializeObject<T>(value);
    }

    public string SerializeObject(object? value, JsonSerializerSettings? settings)
    {
        return JsonConvert.SerializeObject(value, settings);
    }
}
