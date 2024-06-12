using Newtonsoft.Json;
using Unity.Services.Matchmaker.Authoring.Core.Model;

namespace Unity.Services.Cli.Matchmaker.Parser;

public class ResourceNameConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsSubclassOf(typeof(ResourceName));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return Activator.CreateInstance(objectType, reader.Value) ?? throw new InvalidOperationException();
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }
}
