using System;
using Unity.Services.DeploymentApi.Editor;


namespace Unity.Services.Scheduler.Authoring.Core.Model
{
    public static class Statuses
    {
        public static readonly DeploymentStatus FailedToLoad = new("Failed to load", string.Empty, SeverityLevel.Error);

        public static DeploymentStatus GetFailedToFetch(string details)
            => new("Failed to fetch", details, SeverityLevel.Error);
        public static readonly DeploymentStatus Fetching = new("Fetching", string.Empty, SeverityLevel.Info);
        public static DeploymentStatus GetFetched(string detail) => new("Fetched", detail, SeverityLevel.Success);

        public static DeploymentStatus GetFailedToDeploy(string details)
            => new("Failed to deploy", details, SeverityLevel.Error);
        public static DeploymentStatus GetDeploying(string details = null)
            => new("Deploying", details ?? string.Empty, SeverityLevel.Info);
        public static DeploymentStatus GetDeployed(string details)
            => new("Deployed", details, SeverityLevel.Success);

        public static DeploymentStatus GetFailedToLoad(Exception e, string path)
            => new("Failed to load", $"Failed to load '{path}'. Reason: {e.Message}", SeverityLevel.Error);

        public static DeploymentStatus GetFailedToRead(Exception e, string path)
            => new("Failed to read", $"Failed to read '{path}'. Reason: {e.Message}", SeverityLevel.Error);

        public static DeploymentStatus GetFailedToWrite(Exception e, string path)
            => new("Failed to write", $"Failed to write '{path}'. Reason: {e.Message}", SeverityLevel.Error);

        public static DeploymentStatus GetFailedToSerialize(Exception e, string path)
            => new("Failed to serialize", $"Failed to serialize '{path}'. Reason: {e.Message}", SeverityLevel.Error);

        public static DeploymentStatus GetFailedToDelete(Exception e, string path)
            => new("Failed to serialize", $"Failed to delete '{path}'. Reason: {e.Message}", SeverityLevel.Error);
    }
}
