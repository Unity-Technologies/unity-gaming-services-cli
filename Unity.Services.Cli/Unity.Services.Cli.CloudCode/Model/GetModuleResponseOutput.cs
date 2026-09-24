using System.Globalization;
using Newtonsoft.Json;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudCode.Model;

class GetModuleResponseOutput
{
    public string Name { get; }
    public string Language { get; }
    public string DateModified { get; }
    public string DateCreated { get; }
    public Dictionary<string,string>? Tags { get; }
    public string? SignedDownloadUrl { get; }
    public bool? HasError { get; }
    public string? ErrorMessage { get; }
    public object? Endpoints { get; }

    /// <summary>
    /// Null unless the caller asked for versions. It is also null when the response carries none,
    /// which means module versions are not enabled for the project rather than that the module has
    /// no versions.
    /// </summary>
    /// <remarks>
    /// The -j output serialises this object directly rather than going through ToString(), so the
    /// null must be suppressed explicitly: otherwise every `modules get -j` without --versions
    /// would gain a "Versions": null key it did not have before, and a JSON consumer could not tell
    /// "not requested" from "versions are disabled for this project".
    /// </remarks>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<ModuleVersionOutput>? Versions { get; }

    public GetModuleResponseOutput(GetModuleResponse response, bool includeVersions = false)
    {
        Name = response.Name;
        Language = response.Language;
        DateModified = response.DateModified.ToString("s", CultureInfo.InvariantCulture);
        DateCreated = response.DateCreated.ToString("s", CultureInfo.InvariantCulture);
        Tags = response.Tags;
        SignedDownloadUrl = response.SignedDownloadURL;
        HasError = response.HasError;
        ErrorMessage = HasError == true ? response.ErrorMessage : null;
        Endpoints = response.Endpoints;
        Versions = includeVersions && response.Versions != null
            ? response.Versions.Select(version => new ModuleVersionOutput(version)).ToList()
            : null;
    }

    public override string ToString()
    {
        var data = new Dictionary<string, object?>
        {
            { "name", Name },
            { "language", Language },
            { "dateModified", DateModified },
            { "dateCreated", DateCreated }
        };
        if (Tags != null && Tags.Count > 0)
            data["tags"] = Tags;
        if (!string.IsNullOrEmpty(SignedDownloadUrl))
            data["signedDownloadURL"] = SignedDownloadUrl;
        if (HasError.HasValue)
            data["hasError"] = HasError.Value;
        if (HasError == true && !string.IsNullOrEmpty(ErrorMessage))
            data["errorMessage"] = ErrorMessage;
        if (Endpoints is not null)
            data["endpoints"] = Endpoints;
        if (Versions is not null)
            data["versions"] = Versions.Select(version => version.ToDictionary()).ToList();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(data);
    }
}
