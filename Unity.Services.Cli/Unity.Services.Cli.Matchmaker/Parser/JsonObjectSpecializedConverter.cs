using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model;

namespace Unity.Services.Cli.Matchmaker.Parser;

/// <summary>
/// This converter stores the raw json string for complex object that we don't care to deserialize and the client generator can't handle
/// It can take a json object, array, string or number literal and output it back without messing the type
/// </summary>
class JsonObjectSpecializedConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is JsonObject valueJson)
        {
            var raw = valueJson.Value;
            if (string.IsNullOrWhiteSpace(raw))
            {
                writer.WriteNull();
                return;
            }
            try
            {
                var token = JToken.Parse(raw);
                token.WriteTo(writer);
            }
            catch (JsonReaderException)
            {
                writer.WriteValue(raw);
            }
        }
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        var obj = JToken.ReadFrom(reader);
        if (obj.Type == JTokenType.String)
            return new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized("\"" + obj + "\"");
        return new AdminApiClient.MatchmakerAdminClient.JsonObjectSpecialized(obj.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsAssignableTo(typeof(JsonObject));
    }
}
