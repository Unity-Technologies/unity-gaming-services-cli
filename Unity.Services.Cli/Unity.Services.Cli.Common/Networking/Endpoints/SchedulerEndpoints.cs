namespace Unity.Services.Cli.Common.Networking;

public class SchedulerEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.api.unity.com";

    protected override string Staging { get; } = "https://staging.services.api.unity.com";
}
