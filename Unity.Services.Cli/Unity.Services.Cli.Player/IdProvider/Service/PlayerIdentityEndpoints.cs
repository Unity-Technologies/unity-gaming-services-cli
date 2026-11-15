using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.Player.IdProvider.Service;

/// <summary>
/// The services gateway the Unity Dashboard and the Authentication package's Editor settings use for identity
/// provider administration. The public Player Auth Admin API has no identity provider routes.
/// </summary>
public class PlayerIdentityEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.unity.com";

    protected override string Staging { get; } = "https://staging.services.unity.com";
}
