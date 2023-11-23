using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.Deploy;


public class LeaderboardPatchConverter : JsonConverter
{
    public override bool CanRead => false;

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsAssignableTo(typeof(LeaderboardPatchConfig));
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        JObject jsonObject = new JObject();
        var properties = value!.GetType().GetProperties();
        var clearableValues = new [] { nameof(LeaderboardPatchConfig.TieringConfig), nameof(LeaderboardPatchConfig.ResetConfig) };

        // The patch will only clear an nested object if the object is serialized to `{}`.
        // This makes any generated client fail to actually clear the nested object impossible
        // without customizing json serialization
        foreach (var property in properties)
        {
            object? propertyValue = property.GetValue(value);
            if (propertyValue == null && clearableValues.Contains(property.Name))
            {
                jsonObject.Add(property.Name, new JObject());
            }
            else if (propertyValue == null)
            {
                jsonObject.Add(property.Name, JValue.CreateNull());
            }
            else
            {
                jsonObject.Add(property.Name, JToken.FromObject(propertyValue!, serializer));
            }
        }

        jsonObject.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException("Deserialization is not needed");
    }
}

