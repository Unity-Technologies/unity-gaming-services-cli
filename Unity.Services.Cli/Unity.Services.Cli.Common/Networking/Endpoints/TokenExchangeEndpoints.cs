namespace Unity.Services.Cli.Common.Networking;

public class TokenExchangeEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.unity.com";

    protected override string Staging { get; } = "https://staging.services.unity.com";
}
