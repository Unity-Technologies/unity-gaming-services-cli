using System.Globalization;
using Newtonsoft.Json;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Model;

class ScriptVersionOutput
{
    // As on ModuleVersionOutput: -j serialises these properties directly, so the nulls must be
    // suppressed for the JSON omissions to match the YAML ones built by ToDictionary().
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? Version { get; }

    public bool IsDraft { get; }
    public string DateUpdated { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DateCreated { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Tags { get; }

    public ScriptVersionOutput(GetScriptResponseVersionsInner version)
    {
        Version = version._Version;
        IsDraft = version.IsDraft;
        DateUpdated = version.DateUpdated.ToString("s", CultureInfo.InvariantCulture);
        DateCreated = version.DateCreated == default
            ? null
            : version.DateCreated.ToString("s", CultureInfo.InvariantCulture);

        var userTags = version.Tags?
            .Where(tag => !tag.Key.StartsWith(CloudCodeConstants.ReservedTagPrefix, StringComparison.Ordinal))
            .ToDictionary(tag => tag.Key, tag => tag.Value);
        Tags = userTags is { Count: > 0 } ? userTags : null;
    }

    internal Dictionary<string, object?> ToDictionary()
    {
        var data = new Dictionary<string, object?>();
        // The working copy is the only entry with a null version, and the only one with
        // isDraft true. Its version key is omitted rather than rendered empty.
        if (Version.HasValue)
            data["version"] = Version.Value;
        data["isDraft"] = IsDraft;
        data["dateUpdated"] = DateUpdated;
        if (!string.IsNullOrEmpty(DateCreated))
            data["dateCreated"] = DateCreated;
        if (Tags is { Count: > 0 })
            data["tags"] = Tags;
        return data;
    }
}
