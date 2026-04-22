using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.Services.Cli.Matchmaker.Parser
{
    class FilterValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // Match any nested type named FilterValue to avoid needing direct reference
            return objectType.Name == "FilterValue" && objectType.IsClass;
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var token = JToken.Load(reader);

            var ctors = objectType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            object? instance = null;

            object? TryInvoke(Func<object?> build)
            {
                try { return build(); } catch { return null; }
            }

            if (token.Type == JTokenType.Integer)
            {
                var intCtor = ctors.FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(int));
                instance = TryInvoke(() => intCtor?.Invoke(new object[] { token.Value<int>() }));
                if (instance != null) return instance;
            }
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            {
                var floatCtor = ctors.FirstOrDefault(c => c.GetParameters().Length == 1 && (c.GetParameters()[0].ParameterType == typeof(float) || c.GetParameters()[0].ParameterType == typeof(double)));
                instance = TryInvoke(() => floatCtor?.Invoke(new object[] { token.Value<double>() }));
                if (instance != null) return instance;
            }
            if (token.Type == JTokenType.String)
            {
                var stringCtor = ctors.FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(string));
                instance = TryInvoke(() => stringCtor?.Invoke(new object[] { token.Value<string>() ?? string.Empty }));
                if (instance != null) return instance;
            }

            var anyStringCtor = ctors.FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(string));
            instance = TryInvoke(() => anyStringCtor?.Invoke(new object[] { token.ToString(Formatting.None) }));
            if (instance != null) return instance;

            var first = ctors.FirstOrDefault();
            var param = first?.GetParameters().FirstOrDefault();
            if (param != null)
            {
                try
                {
                    var converted = Convert.ChangeType(token.ToString(), param.ParameterType);
                    instance = first!.Invoke(new[] { converted });
                }
                catch { }
            }

            return instance;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var valType = value.GetType();
            object? primitive = null;
            foreach (var prop in valType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;
                var propVal = prop.GetValue(value);
                if (propVal == null) continue;
                var t = prop.PropertyType;
                if (t == typeof(string) || t == typeof(int) || t == typeof(float) || t == typeof(double))
                {
                    primitive = propVal;
                    break;
                }
            }
            if (primitive == null)
            {
                foreach (var field in valType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var fieldVal = field.GetValue(value);
                    if (fieldVal == null) continue;
                    var t = field.FieldType;
                    if (t == typeof(string) || t == typeof(int) || t == typeof(float) || t == typeof(double))
                    {
                        primitive = fieldVal;
                        break;
                    }
                }
            }

            switch (primitive)
            {
                case int i:
                    writer.WriteValue(i); return;
                case float f:
                    writer.WriteValue(f); return;
                case double d:
                    writer.WriteValue(d); return;
                case string s:
                    if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var si)) { writer.WriteValue(si); return; }
                    if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var sd)) { writer.WriteValue(sd); return; }
                    writer.WriteValue(s); return;
            }

            var str = value.ToString();
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i2))
            {
                writer.WriteValue(i2); return;
            }
            if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d2))
            {
                writer.WriteValue(d2); return;
            }
            writer.WriteValue(str);
        }
    }
}
