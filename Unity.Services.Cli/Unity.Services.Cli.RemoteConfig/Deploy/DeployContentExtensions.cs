using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

public static class DeployContentExtensions
{
    public static ICollection<DeployContent> GetUniqueDescriptions(this ICollection<DeployContent> contents)
    {
        var merged = new Dictionary<string, DeployContent>();
        foreach (var content in contents)
        {
            var key = content.Detail;
            if (merged.ContainsKey(key))
            {
                var existing = merged[key];
                merged[key] = new DeployContent($"{existing.Name}' '{content.Name}", existing.Type, existing.Path, existing.Progress, existing.Status, existing.Detail);
            }
            else
            {
                merged.Add(key, content);
            }
        }
        return merged.Values;
    }
}
