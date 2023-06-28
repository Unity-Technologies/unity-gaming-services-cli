namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// Endpoints used to reach the Unity Remote Config Service.
/// </summary>
public sealed class RemoteConfigEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.api.unity.com";

    protected override string Staging { get; } = "https://staging.services.api.unity.com";
}

/// <summary>
/// Endpoints used to reach the Unity Remote Config Service's internal admin APIs.
/// </summary>
public sealed class RemoteConfigInternalEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.unity.com";

    protected override string Staging { get; } = "https://staging.services.unity.com";
}
