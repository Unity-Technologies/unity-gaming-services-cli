using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.Player.Networking;

public class PlayerAuthEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://player-auth.services.api.unity.com";

    protected override string Staging { get; } = "https://player-auth-stg.services.api.unity.com";
}
