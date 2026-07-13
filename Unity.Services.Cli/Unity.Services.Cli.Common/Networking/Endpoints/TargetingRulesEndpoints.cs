namespace Unity.Services.Cli.Common.Networking;
public class TargetingRulesEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.api.unity.com/targeting-rules";

    protected override string Staging { get; } = "https://staging.services.api.unity.com/targeting-rules";

}
