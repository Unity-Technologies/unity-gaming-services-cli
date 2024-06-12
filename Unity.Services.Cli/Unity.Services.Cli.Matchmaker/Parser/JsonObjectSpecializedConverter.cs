using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Matchmaker.Authoring.Core.Model;

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
            var obj = JToken.Parse(valueJson.Value);
            obj.WriteTo(writer);
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
