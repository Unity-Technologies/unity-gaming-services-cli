using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Player.IdProvider.Registry;

namespace Unity.Services.Cli.Player.IdProvider.Models;

/// <summary>
/// An identity provider configuration as the commands report it. Carries whether a client secret is stored,
/// never the secret itself. Optional fields the API leaves empty are null, never <c>""</c>.
/// </summary>
public class IdProviderResult
{
    [JsonProperty("configId")]
    public string? ConfigId { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    [JsonProperty("clientId")]
    public string? ClientId { get; set; }

    [JsonProperty("enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("hasClientSecret")]
    public bool HasClientSecret { get; set; }

    [JsonProperty("relyingParty")]
    public string? RelyingParty { get; set; }

    [JsonProperty("issuer")]
    public string? Issuer { get; set; }

    [JsonProperty("additionalAppIds")]
    public List<AdditionalAppId>? AdditionalAppIds { get; set; }

    public static IdProviderResult From(IdProviderResponse response)
    {
        return new IdProviderResult
        {
            ConfigId = response.ConfigId,
            Type = response.Type,
            Name = IdProviderTypes.Find(response.Type)?.DisplayName,
            DisplayName = NullIfEmpty(response.DisplayName),
            ClientId = NullIfEmpty(response.ClientId),
            Enabled = !response.Disabled,
            HasClientSecret = response.HasClientSecret,
            RelyingParty = NullIfEmpty(response.RelyingParty),
            Issuer = NullIfEmpty(response.OidcConfig?.Issuer),
            AdditionalAppIds = response.SteamConfig?.AdditionalAppIds,
        };
    }

    static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    public override string ToString() => IdProviderJson.Indented(this);
}

public class IdProviderResultList : List<IdProviderResult>
{
    public IdProviderResultList(IEnumerable<IdProviderResult> results)
        : base(results) { }

    public override string ToString() => IdProviderJson.Indented(this);
}

/// <summary>
/// A supported identity provider type and the fields configuring it takes.
/// </summary>
public class IdProviderTypeResult
{
    [JsonProperty("type")]
    public string Type { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("needsClientId")]
    public bool NeedsClientId { get; set; }

    [JsonProperty("needsClientSecret")]
    public bool NeedsClientSecret { get; set; }

    [JsonProperty("supportsAdditionalAppIds")]
    public bool SupportsAdditionalAppIds { get; set; }

    [JsonProperty("canBeCreated")]
    public bool CanBeCreated { get; set; }

    [JsonProperty("canBeDeleted")]
    public bool CanBeDeleted { get; set; }

    [JsonProperty("clientIdLabel")]
    public string ClientIdLabel { get; set; } = "";

    [JsonProperty("clientSecretLabel")]
    public string ClientSecretLabel { get; set; } = "";

    public static IdProviderTypeResult From(IdProviderType definition)
    {
        return new IdProviderTypeResult
        {
            Type = definition.Type,
            Name = definition.DisplayName,
            NeedsClientId = definition.NeedsClientId,
            NeedsClientSecret = definition.NeedsClientSecret,
            SupportsAdditionalAppIds = definition.SupportsAdditionalAppIds,
            CanBeCreated = definition.CanBeCreated,
            CanBeDeleted = definition.CanBeDeleted,
            ClientIdLabel = definition.ClientIdLabel,
            ClientSecretLabel = definition.ClientSecretLabel,
        };
    }

    public override string ToString() => IdProviderJson.Indented(this);
}

public class IdProviderTypeResultList : List<IdProviderTypeResult>
{
    public IdProviderTypeResultList(IEnumerable<IdProviderTypeResult> results)
        : base(results) { }

    public override string ToString() => IdProviderJson.Indented(this);
}

/// <summary>
/// The outcome of a command that changes, or with <c>--dry-run</c> would change, an identity provider.
/// </summary>
public class IdProviderChangeResult
{
    public const string DryRun = "dry-run";
    public const string Applied = "applied";

    [JsonProperty("status")]
    public string Status { get; set; } = "";

    [JsonProperty("configId")]
    public string? ConfigId { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; } = "";

    [JsonProperty("message")]
    public string Message { get; set; } = "";

    [JsonProperty("provider")]
    public IdProviderResult? Provider { get; set; }

    public override string ToString() => IdProviderJson.Indented(this);
}

static class IdProviderJson
{
    public static string Indented(object value)
    {
        return JToken.Parse(JsonConvert.SerializeObject(value)).ToString(Formatting.Indented);
    }
}
