namespace Unity.Services.Cli.Common.Networking;

/// <summary>
/// Endpoints used to reach the Unity Telemetry Services.
/// </summary>
public class TelemetryApiEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://operate-sdk-telemetry.unity3d.com/";

    protected override string Staging { get; } = "https://sdk-telemetry.stg.mz.internal.unity3d.com/";
}
