using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

public static class Keys
{
    public const string FleetIdKey = "fleets";

    public const long ValidBuildConfigurationId = 1;
    public const string ValidFleetId = "00000000-0000-0000-1000-000000000000";
    public const string ValidRegionId = "00000000-0000-0000-0000-100000000000";
    public const string ValidTemplateRegionId = "00000000-0000-0000-0000-1a0000000000";
    public const long ValidBuildId = 1;
    public const string ValidBucketId = "00000000-0000-0000-0000-000000000000";
    public const string ValidReleaseId = "00000000-0000-0000-0000-000000000000";

    public const string ValidServerId = "123";

    public const string ProjectPathPart =
        $"projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}";

    public const string FleetsPath = $"/multiplay/fleets/v1/{ProjectPathPart}/fleets";
    public const string ServersPath = $"/multiplay/servers/v1/{ProjectPathPart}/servers";

    public const string ValidFleetPath = $"{FleetsPath}/{ValidFleetId}";
    public const string ValidServersPath = $"{ServersPath}/{ValidServerId}";

    public const string BuildsPath = $"/multiplay/builds/v1/{ProjectPathPart}/builds";
    public static readonly string ValidBuildPath = $"{BuildsPath}/{ValidBuildId}";
    public static readonly string ValidBuildInstallsPath = $"{ValidBuildPath}/installs";
}
