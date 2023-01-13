using System.Reflection;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Common.Models;

[Serializable]
public class Configuration
{
    [JsonProperty(Keys.ConfigKeys.EnvironmentName)]
    public string? EnvironmentName { get; set; }

    [JsonProperty(Keys.ConfigKeys.ProjectId)]
    public string? CloudProjectId { get; set; }

    public string? GetValue(string key)
    {
        return GetType()
            .GetProperties()
            .First(property => GetJsonPropertyName(property) == key)
            .GetValue(this) as string;
    }

    public void SetValue(string key, string value)
    {
        GetType()
            .GetProperties()
            .First(property => GetJsonPropertyName(property) == key)
            .SetValue(this, value);
    }

    public void DeleteValue(string key)
    {
        GetType()
            .GetProperties()
            .First(property => GetJsonPropertyName(property) == key)
            .SetValue(this, null);
    }

    public IEnumerable<(string? key, string? value)> List()
    {
        return GetType()
            .GetProperties()
            .Select(property => (GetJsonPropertyName(property), property.GetValue(this) as string));
    }

    /// <summary>
    /// Get the supported configuration keys
    /// </summary>
    /// <returns></returns>
    public static IList<string?> GetKeys() => typeof(Configuration)
        .GetProperties().Select(GetJsonPropertyName).ToList();

    static string? GetJsonPropertyName(PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName;
    }
}
