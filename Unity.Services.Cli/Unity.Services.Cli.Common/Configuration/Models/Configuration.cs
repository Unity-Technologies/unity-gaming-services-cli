using Newtonsoft.Json;

namespace Unity.Services.Cli.Common.Models;

[Serializable]
public class Configuration
{
    [JsonProperty(Keys.ConfigKeys.EnvironmentName)]
    public string? EnvironmentName { get; set; }

    [JsonProperty(Keys.ConfigKeys.ProjectId)]
    public string? CloudProjectId { get; set; }

    [JsonProperty(Keys.ConfigKeys.BucketName)]
    public string? CloudBucketName { get; set; }


    public string? GetValue(string key)
    {
        return key switch
        {
            Keys.ConfigKeys.EnvironmentName => EnvironmentName,
            Keys.ConfigKeys.ProjectId => CloudProjectId,
            Keys.ConfigKeys.BucketName => CloudBucketName,
            _ => throw new ArgumentException($"Unknown configuration key: {key}", nameof(key))
        };
    }

    public void SetValue(string key, string value)
    {
        switch (key)
        {
            case Keys.ConfigKeys.EnvironmentName:
                EnvironmentName = value;
                break;
            case Keys.ConfigKeys.ProjectId:
                CloudProjectId = value;
                break;
            case Keys.ConfigKeys.BucketName:
                CloudBucketName = value;
                break;
            default:
                throw new ArgumentException($"Unknown configuration key: {key}", nameof(key));
        }
    }

    public void DeleteValue(string key)
    {
        switch (key)
        {
            case Keys.ConfigKeys.EnvironmentName:
                EnvironmentName = null;
                break;
            case Keys.ConfigKeys.ProjectId:
                CloudProjectId = null;
                break;
            case Keys.ConfigKeys.BucketName:
                CloudBucketName = null;
                break;
            default:
                throw new ArgumentException($"Unknown configuration key: {key}", nameof(key));
        }
    }

    public IEnumerable<(string? key, string? value)> List()
    {
        yield return (Keys.ConfigKeys.EnvironmentName, EnvironmentName);
        yield return (Keys.ConfigKeys.ProjectId, CloudProjectId);
        yield return (Keys.ConfigKeys.BucketName, CloudBucketName);
    }

    /// <summary>
    /// Get the supported configuration keys
    /// </summary>
    /// <returns></returns>
    public static IList<string?> GetKeys()
    {
        return Keys.ConfigKeys.Keys.ToList<string?>();
    }
}
