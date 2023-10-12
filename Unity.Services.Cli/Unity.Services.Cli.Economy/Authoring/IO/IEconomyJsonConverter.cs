using Newtonsoft.Json;

namespace Unity.Services.Cli.Economy.Authoring.IO;

interface IEconomyJsonConverter
{
    public T? DeserializeObject<T>(string value);

    public string SerializeObject(object? value, JsonSerializerSettings? settings);
}
