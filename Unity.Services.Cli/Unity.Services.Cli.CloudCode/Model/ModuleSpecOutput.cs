using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Unity.Services.Cli.CloudCode.Model;

[JsonConverter(typeof(ModuleSpecOutputConverter))]
class ModuleSpecOutput
{
    readonly string m_RawYaml;
    internal readonly object? ParsedContent;

    public ModuleSpecOutput(string yaml)
    {
        m_RawYaml = yaml;
        var deserializer = new DeserializerBuilder().Build();
        ParsedContent = deserializer.Deserialize<object>(yaml);
    }

    public override string ToString() => m_RawYaml;
}

class ModuleSpecOutputConverter : JsonConverter<ModuleSpecOutput>
{
    public override void WriteJson(JsonWriter writer, ModuleSpecOutput? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value?.ParsedContent);
    }

    public override ModuleSpecOutput ReadJson(
        JsonReader reader, Type objectType, ModuleSpecOutput? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
