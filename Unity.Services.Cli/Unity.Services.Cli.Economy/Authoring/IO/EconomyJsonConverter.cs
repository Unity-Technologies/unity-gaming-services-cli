using Newtonsoft.Json;
using Unity.Services.Cli.Economy.Templates;
using Unity.Services.Economy.Editor.Authoring.Core.IO;

namespace Unity.Services.Cli.Economy.Authoring.IO;

class EconomyJsonConverter : IEconomyJsonConverter
{
    public T? DeserializeObject<T>(string value)
    {
        return JsonConvert.DeserializeObject<T>(value);
    }

    public string SerializeObject(object? value)
    {
        return JsonConvert.SerializeObject(value, EconomyResourceFile.GetSerializationSettings());
    }
}
