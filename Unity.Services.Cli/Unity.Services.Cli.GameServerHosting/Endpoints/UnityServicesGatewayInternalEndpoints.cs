using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.GameServerHosting.Endpoints;

public class UnityServicesGatewayInternalEndpoints : NetworkTargetEndpoints
{
    // For this we only allow it to work in staging
    protected override string Prod { get; } = "http://localhost:8080";

    protected override string Staging { get; } = "https://staging.services.unity.com";
}
