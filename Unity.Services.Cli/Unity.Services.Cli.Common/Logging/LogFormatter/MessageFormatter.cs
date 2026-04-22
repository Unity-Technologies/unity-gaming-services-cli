using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.Common.Logging;

static class MessageFormatter
{
    static readonly ISerializer k_YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Formats a JSON string with proper indentation for improved readability.
    /// </summary>
    /// <param name="message">The JSON string to format. Can be null, empty, or whitespace.</param>
    /// <returns>
    /// A formatted JSON string with indentation if the input is valid JSON;
    /// otherwise returns the original input unchanged.
    /// </returns>
    public static string TryFormatAsJson(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        try
        {
            var jsonObject = JToken.Parse(message);
            return jsonObject.ToString(Formatting.Indented);
        }
        catch (Exception)
        {
            return message;
        }
    }

    /// <summary>
    /// Converts a JSON string to YAML format.
    /// </summary>
    /// <param name="message">The JSON string to convert to YAML. Can be null, empty, or whitespace.</param>
    /// <returns>
    /// A YAML-formatted string if the input is valid JSON;
    /// otherwise returns the original input unchanged.
    /// </returns>
    public static string TryFormatAsYaml(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        try
        {
            var obj = JsonConvert.DeserializeObject<ExpandoObject>(message, new ExpandoObjectConverter());
            var yamlOutput = k_YamlSerializer.Serialize(obj);
            return yamlOutput;
        }
        catch (Exception)
        {
            return message;
        }
    }
}
