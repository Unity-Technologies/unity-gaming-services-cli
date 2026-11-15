using System.Globalization;
using Newtonsoft.Json;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Model;

class ModuleVersionOutput
{
    public long Version { get; }
    public bool IsLive { get; }

    // The -j output serialises these properties directly, where the YAML path goes through
    // ToDictionary() and omits absent keys. Without this the two formats disagree: a version with no
    // user tags would emit "Tags": null in JSON while YAML omitted it entirely.
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DateCreated { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Tags { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? ReleaseIds { get; }

    public ModuleVersionOutput(ModuleVersion version)
    {
        Version = version._Version;
        IsLive = version.IsLive;
        DateCreated = version.DateCreated?.ToString("s", CultureInfo.InvariantCulture);

        Tags = version.Tags is { Count: > 0 } ? new Dictionary<string, string>(version.Tags) : null;
        ReleaseIds = version.ReleaseIds is { Count: > 0 } ? new List<string>(version.ReleaseIds) : null;
    }

    internal Dictionary<string, object?> ToDictionary()
    {
        var data = new Dictionary<string, object?>
        {
            { "version", Version },
            { "isLive", IsLive }
        };
        if (!string.IsNullOrEmpty(DateCreated))
            data["dateCreated"] = DateCreated;
        if (Tags is { Count: > 0 })
            data["tags"] = Tags;
        if (ReleaseIds is { Count: > 0 })
            data["releaseIds"] = ReleaseIds;
        return data;
    }
}
