using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.Matchmaker.Service;

public class AdminApiTargetEndpoint : NetworkTargetEndpoints
{

    public AdminApiTargetEndpoint() { }

    protected override string Prod { get; } = "https://services.api.unity.com/";

    protected override string Staging { get; } = "https://staging.services.api.unity.com/";
}
