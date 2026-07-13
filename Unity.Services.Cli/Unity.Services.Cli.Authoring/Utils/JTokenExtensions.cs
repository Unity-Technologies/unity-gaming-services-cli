using Newtonsoft.Json.Linq;

namespace Unity.Services.Cli.Authoring.Utils;

/// <summary>
/// Extension methods for converting Newtonsoft.Json JToken types to native .NET objects.
/// </summary>
public static class JTokenExtensions
{
    public static object ToValue(this JToken token)
    {
        return token.Type switch
        {
            JTokenType.Null => null!,
            JTokenType.String => ((JValue)token).Value!,
            JTokenType.Integer => ((JValue)token).Value!,
            JTokenType.Float => ((JValue)token).Value!,
            JTokenType.Boolean => ((JValue)token).Value!,
            JTokenType.Date => ((JValue)token).Value!,
            JTokenType.Guid => ((JValue)token).Value!,
            JTokenType.Uri => ((JValue)token).Value!,
            JTokenType.TimeSpan => ((JValue)token).Value!,
            JTokenType.Array => ToList((JArray)token),
            JTokenType.Object => ToDictionary((JObject)token),
            _ => token
        };
    }

    static List<object> ToList(JArray jArray)
    {
        return jArray.Select(ToValue).ToList();
    }

    static Dictionary<string, object> ToDictionary(JObject jObject)
    {
        var dictionary = new Dictionary<string, object>(jObject.Count);
        foreach (var property in jObject.Properties())
        {
            dictionary[property.Name] = ToValue(property.Value);
        }
        return dictionary;
    }
}
