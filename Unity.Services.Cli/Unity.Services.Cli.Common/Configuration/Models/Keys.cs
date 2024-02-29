using System.Collections.ObjectModel;

namespace Unity.Services.Cli.Common.Models;

/// <summary>
/// Class that provides a centralized place to declare and reference keys
/// </summary>
public static class Keys
{
    /// <summary>
    /// Readonly dictionary establishing relations between configuration keys and environment keys
    /// </summary>
    public static readonly ReadOnlyDictionary<string, string> ConfigEnvironmentPairs
        = new(new Dictionary<string, string>
        {
            [ConfigKeys.ProjectId] = EnvironmentKeys.ProjectId,
            [ConfigKeys.EnvironmentName] = EnvironmentKeys.EnvironmentName,
        });

    /// <summary>
    /// Class containing a set of constant keys used for storing and retrieving from configuration
    /// </summary>
    public static class ConfigKeys
    {
        public const string ProjectId = "project-id";
        public const string EnvironmentName = "environment-name";
        // Environment Id is currently not stored/retrieved but the key is used for validation purposes
        public const string EnvironmentId = "environment-id";
        public const string BucketId = "bucket-id";
        public static readonly IReadOnlyList<string> Keys = new List<string> { ProjectId, EnvironmentName, BucketId };
    }

    /// <summary>
    /// Class containing a set of constant keys used for retrieving information from system environment variables
    /// </summary>
    public static class EnvironmentKeys
    {
        public const string ProjectId = "UGS_CLI_PROJECT_ID";
        public const string EnvironmentName = "UGS_CLI_ENVIRONMENT_NAME";
        public const string BucketId = "UGS_CLI_BUCKET_ID";
        public const string TelemetryDisabled = "UGS_CLI_TELEMETRY_DISABLED";
        // Env variables used for identifying the cicd platform being used
        public const string RunningOnDocker = "DOTNET_RUNNING_IN_CONTAINER";
        public const string RunningOnJenkins = "JENKINS_HOME";
        public const string RunningOnGithubActions = "GITHUB_ACTIONS";
        public const string RunningOnUnityCloudBuild = "IS_BUILDER";
        public const string RunningOnYamato = "YAMATO_JOB_ID";

        public static readonly IReadOnlyList<string> Keys = new List<string> { ProjectId, EnvironmentName, TelemetryDisabled };

        public static readonly IReadOnlyList<string> cicdKeys = new List<string>
        {
            RunningOnDocker,
            RunningOnJenkins,
            RunningOnGithubActions,
            RunningOnUnityCloudBuild,
            RunningOnYamato
        };
    }

    /// <summary>
    /// Readonly dictionary establishing relations between cicd environment keys and their display names
    /// </summary>
    public static readonly ReadOnlyDictionary<string, string> CicdEnvVarToDisplayNamePair
        = new(new Dictionary<string, string>
        {
            [EnvironmentKeys.RunningOnDocker] = "Docker",
            [EnvironmentKeys.RunningOnJenkins] = "Jenkins",
            [EnvironmentKeys.RunningOnGithubActions] = "GithubActions",
            [EnvironmentKeys.RunningOnUnityCloudBuild] = "UnityCloudBuild",
            [EnvironmentKeys.RunningOnYamato] = "Yamato"
        });
}
