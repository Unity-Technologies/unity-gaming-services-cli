using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.GameServerHosting.Endpoints;

public class CloudContentDeliveryEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://content-api.cloud.unity3d.com/api/v1";

    protected override string Staging { get; } = "https://content-api-stg.cloud.unity3d.com/api/v1";
}
