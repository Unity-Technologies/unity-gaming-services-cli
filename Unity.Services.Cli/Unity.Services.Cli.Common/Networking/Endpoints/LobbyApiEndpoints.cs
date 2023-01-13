namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// Endpoints used to reach the Lobby service.
/// </summary>
public class LobbyApiEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://lobby.services.api.unity.com/v1";

    protected override string Staging { get; } = "https://lobby-stg.services.api.unity.com/v1";

}
