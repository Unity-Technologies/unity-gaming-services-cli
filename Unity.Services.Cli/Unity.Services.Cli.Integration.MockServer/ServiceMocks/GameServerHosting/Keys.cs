using Unity.Services.Cli.MockServer.Common;

namespace Unity.Services.Cli.MockServer.ServiceMocks.GameServerHosting;

public static class Keys
{
    public const string FleetIdKey = "fleets";

    public const long ValidBuildConfigurationId = 1;
    public const string ValidFleetId = "00000000-0000-0000-1000-000000000000";
    public const string ValidRegionId = "00000000-0000-0000-0000-100000000000";
    public const string ValidRegionIdAlt = "00000000-0000-0000-0000-100000000001";
    public const string ValidFleetRegionId = "00000000-0000-0000-0000-200000000000";
    public const string ValidTemplateRegionId = "00000000-0000-0000-0000-1a0000000000";
    public const long ValidBuildId = 1;
    public const string ValidBucketId = "00000000-0000-0000-0000-000000000000";
    public const string ValidReleaseId = "00000000-0000-0000-0000-000000000000";
    public const long ValidBuildIdBucket = 101;
    public const long ValidBuildIdContainer = 102;
    public const long ValidBuildIdFileUpload = 103;
    public const long ValidMachineId = 654321L;
    public const string ValidServerId = "123";

    public const string ProjectPathPart =
        $"projects/{CommonKeys.ValidProjectId}/environments/{CommonKeys.ValidEnvironmentId}";

    public const string FilesPath = $"/multiplay/files/v1/{ProjectPathPart}/files";
    public const string FleetsPath = $"/multiplay/fleets/v1/{ProjectPathPart}/fleets";
    public const string ServersPath = $"/multiplay/servers/v1/{ProjectPathPart}/servers";
    public const string MachinesPath = $"/multiplay/machines/v1/{ProjectPathPart}/machines";

    public const string ValidFleetPath = $"{FleetsPath}/{ValidFleetId}";
    public const string ValidServersPath = $"{ServersPath}/{ValidServerId}";
    public const string AvailableRegionsPath = $"{ValidFleetPath}/available-regions";
    public const string FleetRegionsPath = $"{ValidFleetPath}/regions";

    public const string BuildsPath = $"/multiplay/builds/v1/{ProjectPathPart}/builds";
    public const string BuildConfigurationsPath = $"/multiplay/build-configurations/v1/{ProjectPathPart}/build-configurations";
    public static readonly string ValidBuildConfigurationPath = $"{BuildConfigurationsPath}/{ValidBuildConfigurationId}";
    public static readonly string ValidBuildPath = $"{BuildsPath}/{ValidBuildId}";
    public static readonly string ValidBuildInstallsPath = $"{ValidBuildPath}/installs";

    public static readonly string ValidBuildPathBucket = $"{BuildsPath}/{ValidBuildIdBucket}";
    public static readonly string ValidBuildPathContainer = $"{BuildsPath}/{ValidBuildIdContainer}";
    public static readonly string ValidBuildPathFileUpload = $"{BuildsPath}/{ValidBuildIdFileUpload}";
    public static readonly string ValidBuildPathFileUploadFiles = $"{BuildsPath}/{ValidBuildIdFileUpload}/files";
}
