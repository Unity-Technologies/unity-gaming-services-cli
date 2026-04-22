namespace Unity.Services.Cli.Common.Networking;

public class LiveReleasesApiEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.api.unity.com/live-releases";

    protected override string Staging { get; } = "https://staging.services.api.unity.com/live-releases";

}
