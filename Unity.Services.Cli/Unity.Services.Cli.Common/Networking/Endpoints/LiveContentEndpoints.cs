namespace Unity.Services.Cli.Common.Networking;

public class LiveContentApiEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.api.unity.com/live-content/admin";

    protected override string Staging { get; } = "https://staging.services.api.unity.com/live-content/admin";
}
