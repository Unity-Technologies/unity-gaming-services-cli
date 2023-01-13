namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// Endpoints used to reach the Unity Services Gateway.
/// </summary>
public sealed class UnityServicesGatewayEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.api.unity.com";

    protected override string Staging { get; } = "https://staging.services.api.unity.com";
}
