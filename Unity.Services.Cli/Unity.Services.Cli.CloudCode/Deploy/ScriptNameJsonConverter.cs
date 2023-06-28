using Newtonsoft.Json;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

// This is needed as the ScriptName only has private fields
class ScriptNameJsonConverter : JsonConverter<ScriptName>
{
    public override ScriptName ReadJson(
        JsonReader reader,
        Type objectType,
        ScriptName existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        return new ScriptName(reader.Value!.ToString());
    }
    public override void WriteJson(JsonWriter writer, ScriptName value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

}
