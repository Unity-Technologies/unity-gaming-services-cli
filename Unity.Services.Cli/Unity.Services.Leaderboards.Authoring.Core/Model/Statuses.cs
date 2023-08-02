using Unity.Services.DeploymentApi.Editor;


namespace Unity.Services.Leaderboards.Authoring.Core.Model
{
    static class Statuses
    {
        public static readonly DeploymentStatus FailedToLoad = new ("Failed to load", string.Empty, SeverityLevel.Error);

        public static DeploymentStatus GetFailedToFetch(string details)
            => new ("Failed to fetch", details, SeverityLevel.Error);
        public static readonly DeploymentStatus Fetching = new ("Fetching", string.Empty, SeverityLevel.Info);
        public static readonly DeploymentStatus Fetched = new ("Fetched", string.Empty, SeverityLevel.Success);

        public static DeploymentStatus GetFailedToDeploy(string details)
            => new ("Failed to deploy", details, SeverityLevel.Error);
        public static readonly DeploymentStatus Deploying = new ( "Deploying", string.Empty, SeverityLevel.Info);
        public static readonly DeploymentStatus Deployed = new ("Deployed",  string.Empty, SeverityLevel.Success);
    }
}
