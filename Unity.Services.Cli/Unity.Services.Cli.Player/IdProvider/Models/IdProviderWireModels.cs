using Newtonsoft.Json;

namespace Unity.Services.Cli.Player.IdProvider.Models;

/// <summary>
/// An identity provider configuration as the player-identity v2 admin API returns it. The API never returns
/// the client secret, only whether one is stored.
/// </summary>
public class IdProviderResponse
{
    [JsonProperty("configId")]
    public string? ConfigId { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("clientId")]
    public string? ClientId { get; set; }

    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    [JsonProperty("relyingParty")]
    public string? RelyingParty { get; set; }

    [JsonProperty("disabled")]
    public bool Disabled { get; set; }

    [JsonProperty("hasClientSecret")]
    public bool HasClientSecret { get; set; }

    [JsonProperty("oidcConfig")]
    public OpenIdConfig? OidcConfig { get; set; }

    [JsonProperty("steamConfig")]
    public SteamProviderConfig? SteamConfig { get; set; }
}

public class CreateIdProviderRequest
{
    [JsonProperty("type")]
    public string Type { get; set; } = "";

    [JsonProperty("clientId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ClientId { get; set; }

    [JsonProperty("clientSecret", NullValueHandling = NullValueHandling.Ignore)]
    public string? ClientSecret { get; set; }

    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string? DisplayName { get; set; }

    [JsonProperty("relyingParty", NullValueHandling = NullValueHandling.Ignore)]
    public string? RelyingParty { get; set; }

    [JsonProperty("oidcConfig", NullValueHandling = NullValueHandling.Ignore)]
    public OpenIdConfig? OidcConfig { get; set; }

    [JsonProperty("steamConfig", NullValueHandling = NullValueHandling.Ignore)]
    public SteamProviderConfig? SteamConfig { get; set; }
}

/// <summary>
/// A partial update: only the fields that are set are sent, and the configuration keeps the rest.
/// </summary>
public class UpdateIdProviderRequest
{
    [JsonProperty("clientId", NullValueHandling = NullValueHandling.Ignore)]
    public string? ClientId { get; set; }

    [JsonProperty("clientSecret", NullValueHandling = NullValueHandling.Ignore)]
    public string? ClientSecret { get; set; }

    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    public string? DisplayName { get; set; }

    [JsonProperty("relyingParty", NullValueHandling = NullValueHandling.Ignore)]
    public string? RelyingParty { get; set; }

    [JsonProperty("oidcConfig", NullValueHandling = NullValueHandling.Ignore)]
    public OpenIdConfig? OidcConfig { get; set; }

    [JsonProperty("steamConfig", NullValueHandling = NullValueHandling.Ignore)]
    public SteamProviderConfig? SteamConfig { get; set; }
}

public class OpenIdConfig
{
    [JsonProperty("issuer")]
    public string? Issuer { get; set; }
}

public class SteamProviderConfig
{
    [JsonProperty("additionalAppIds")]
    public List<AdditionalAppId>? AdditionalAppIds { get; set; }
}

public class AdditionalAppId
{
    [JsonProperty("appId")]
    public string? AppId { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }
}

class ProblemResponse
{
    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("detail")]
    public string? Detail { get; set; }

    [JsonProperty("code")]
    public int? Code { get; set; }

    [JsonProperty("requestId")]
    public string? RequestId { get; set; }
}
