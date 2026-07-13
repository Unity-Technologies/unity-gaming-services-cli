#if FEATURE_HUB_AUTH
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

class HubMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

class HealthCheckData
{
    [JsonPropertyName("health")]
    public bool Health { get; set; }
}

class ConnectInfoData
{
    [JsonPropertyName("initialized")]
    public bool Initialized { get; set; }

    [JsonPropertyName("ready")]
    public bool Ready { get; set; }

    [JsonPropertyName("online")]
    public bool Online { get; set; }

    [JsonPropertyName("loggedIn")]
    public bool LoggedIn { get; set; }

    [JsonPropertyName("workOffline")]
    public bool WorkOffline { get; set; }

    [JsonPropertyName("showLoginWindow")]
    public bool ShowLoginWindow { get; set; }

    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("maintenance")]
    public bool Maintenance { get; set; }
}

class UserInfoData
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("whitelisted")]
    public bool Whitelisted { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("accessTokenExpiration")]
    public long? AccessTokenExpiration { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("primaryOrg")]
    public string PrimaryOrg { get; set; } = "";

    [JsonPropertyName("organizationForeignKeys")]
    public string OrganizationForeignKeys { get; set; } = "";
}

class WindowShowRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("modal")]
    public bool Modal { get; set; }
}
#endif
