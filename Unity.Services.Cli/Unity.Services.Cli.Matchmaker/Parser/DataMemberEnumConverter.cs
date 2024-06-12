using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.Services.Cli.Matchmaker.Parser;

// Enum may come with DataMember Name property. If it's the case serialize/deserialize that value instead.
public class DataMemberEnumConverter : StringEnumConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsEnum;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader == null || objectType == null)
        {
            throw new ArgumentNullException(objectType == null ? nameof(objectType) : nameof(reader));
        }

        var enumString = reader.Value as string;
        if (enumString == null)
        {
            throw new ArgumentNullException(nameof(reader.Value));
        }

        foreach (var name in Enum.GetNames(objectType))
        {
            var fieldInfo = objectType.GetField(name);
            if (fieldInfo == null)
            {
                continue;
            }

            var enumMemberAttribute = ((DataMemberAttribute[])fieldInfo.GetCustomAttributes(typeof(DataMemberAttribute), true)).SingleOrDefault();
            if (enumMemberAttribute != null && enumMemberAttribute.Name == enumString)
            {
                return Enum.Parse(objectType, name);
            }
        }
        return base.ReadJson(reader, objectType, existingValue, serializer) ?? throw new ArgumentNullException(nameof(reader));
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (writer == null || value == null)
        {
            throw new ArgumentNullException(value == null ? nameof(value) : nameof(writer));
        }

        var enumType = value.GetType();
        var name = Enum.GetName(enumType, value);
        if (name == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var fieldInfo = enumType.GetField(name);
        if (fieldInfo == null)
        {
            throw new ArgumentNullException(nameof(fieldInfo));
        }

        var enumMemberAttribute = ((DataMemberAttribute[])fieldInfo.GetCustomAttributes(typeof(DataMemberAttribute), true)).SingleOrDefault();
        if (enumMemberAttribute != null)
        {
            writer.WriteValue(enumMemberAttribute.Name);
        }
        else
        {
            writer.WriteValue(name);
        }
    }
}
