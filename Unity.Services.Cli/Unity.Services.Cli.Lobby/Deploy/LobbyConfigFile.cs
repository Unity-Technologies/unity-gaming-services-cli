using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.Lobby.Handlers;

namespace Unity.Services.Cli.Lobby.Deploy;

class LobbyConfigFile : IFileTemplate
{
    internal const string k_SchemaUrl = "https://ugs-config-schemas.unity3d.com/v1/lobby-config.schema.json";

    static readonly string k_FileBody = JsonConvert.SerializeObject(
        new LobbyConfigFileContent
        {
            Schema = k_SchemaUrl,
            SchemaId = LobbyConstants.SchemaIdV3,
            ActiveLifespanSeconds = 30,
            DisconnectRemovalTimeSeconds = 120,
            DisconnectHostMigrationTimeSeconds = 10,
            PlayerSlots = new PlayerSlotsConfig
            {
                Minimum = 1,
                Maximum = 150
            },
            SocialProfilesEnabled = false
        },
        Formatting.Indented) + "\n";

    public string Extension => LobbyConstants.FileExtension;

    public string FileBodyText => k_FileBody;
}

class LobbyConfigFileContent
{
    [JsonProperty("$schema")]
    public string? Schema { get; set; }

    [JsonProperty("schemaId")]
    public string? SchemaId { get; set; }

    [JsonProperty("activeLifespanSeconds")]
    public int ActiveLifespanSeconds { get; set; }

    [JsonProperty("disconnectRemovalTimeSeconds")]
    public int DisconnectRemovalTimeSeconds { get; set; }

    [JsonProperty("disconnectHostMigrationTimeSeconds")]
    public int DisconnectHostMigrationTimeSeconds { get; set; }

    [JsonProperty("playerSlots")]
    public PlayerSlotsConfig PlayerSlots { get; set; } = new();

    [JsonProperty("socialProfilesEnabled")]
    public bool? SocialProfilesEnabled { get; set; }

    internal LobbyConfigFileContent() { }

    internal LobbyConfigFileContent(string? schema, string? schemaId, JObject config)
    {
        Schema = schema;
        SchemaId = schemaId;
        ActiveLifespanSeconds = config.Value<int>(LobbyConstants.ActiveLifespanSecondsKey);
        DisconnectRemovalTimeSeconds = config.Value<int>(LobbyConstants.DisconnectRemovalTimeSecondsKey);
        DisconnectHostMigrationTimeSeconds = config.Value<int>(LobbyConstants.DisconnectHostMigrationTimeSecondsKey);
        PlayerSlots = new PlayerSlotsConfig
        {
            Minimum = config[LobbyConstants.PlayerSlotsKey]?.Value<int>(LobbyConstants.PlayerSlotsMinimumKey) ?? 0,
            Maximum = config[LobbyConstants.PlayerSlotsKey]?.Value<int>(LobbyConstants.PlayerSlotsMaximumKey) ?? 0
        };
        SocialProfilesEnabled = config.Value<bool?>(LobbyConstants.SocialProfilesEnabledKey);
    }

    internal JObject ToConfigJObject()
    {
        var obj = new JObject
        {
            [LobbyConstants.ActiveLifespanSecondsKey] = ActiveLifespanSeconds,
            [LobbyConstants.DisconnectRemovalTimeSecondsKey] = DisconnectRemovalTimeSeconds,
            [LobbyConstants.DisconnectHostMigrationTimeSecondsKey] = DisconnectHostMigrationTimeSeconds,
            [LobbyConstants.PlayerSlotsKey] = new JObject
            {
                [LobbyConstants.PlayerSlotsMinimumKey] = PlayerSlots.Minimum,
                [LobbyConstants.PlayerSlotsMaximumKey] = PlayerSlots.Maximum
            }
        };

        if (SocialProfilesEnabled.HasValue)
        {
            obj[LobbyConstants.SocialProfilesEnabledKey] = SocialProfilesEnabled.Value;
        }

        return obj;
    }
}

class PlayerSlotsConfig
{
    [JsonProperty("minimum")]
    public int Minimum { get; set; }

    [JsonProperty("maximum")]
    public int Maximum { get; set; }
}
