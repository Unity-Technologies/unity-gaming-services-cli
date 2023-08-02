using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.Model;

public static class Statuses
{
    public const string FailedToRead = "Failed To Read";
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Loading = "Loading";
    public const string Loaded = "Loaded";
    public const string Deployed = "Deployed";
    public const string Fetched = "Fetched";
    public static DeploymentStatus GetFailedToFetch(string messageDetail) => new DeploymentStatus("Failed to fetch", messageDetail, SeverityLevel.Error);
}
