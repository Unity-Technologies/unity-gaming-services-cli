using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Matchmaker.Authoring.Core.Model;
namespace Unity.Services.Cli.Matchmaker.Parser;

public class MatchHostingConfigTypeConverted : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IMatchHostingConfig);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject item = JObject.Load(reader);
        var type = item["type"]?.Value<string>() ?? "Unspecified";
        switch (type)
        {
            case "Multiplay":
                return item.ToObject<MultiplayConfig>(serializer) ?? throw new InvalidOperationException();
            case "MatchId":
                return item.ToObject<MatchIdConfig>(serializer) ?? throw new InvalidOperationException();
            default:
                throw new JsonSerializationException($"Invalid hosting config type: {type}");
        }
    }

    // Interface cannot be serialized
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) { }
}
