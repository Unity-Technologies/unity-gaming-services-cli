namespace Unity.Services.Cli.Lobby.Handlers;

static class LobbyConstants
{
    internal const string ServiceType = "Lobby";

    internal const string ServiceName = "lobby";

    internal const string ConfigType = "lobby";

    internal const string SchemaId = "lobby";

    internal const string SchemaIdV2 = "lobbyv2";

    internal const string SchemaIdV3 = "lobbyv3";

    internal const string ConfigKey = "lobbyConfig";

    internal const string FileExtension = ".lo";

    internal const string FileName = "lobby.lo";

    internal const string ConfigDisplayName = "Lobby configuration";

    internal const string ActiveLifespanSecondsKey = "activeLifespanSeconds";
    internal const string DisconnectRemovalTimeSecondsKey = "disconnectRemovalTimeSeconds";
    internal const string DisconnectHostMigrationTimeSecondsKey = "disconnectHostMigrationTimeSeconds";
    internal const string PlayerSlotsKey = "playerSlots";
    internal const string PlayerSlotsMinimumKey = "minimum";
    internal const string PlayerSlotsMaximumKey = "maximum";
    internal const string SocialProfilesEnabledKey = "socialProfilesEnabled";
}
