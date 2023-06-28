using Unity.Services.DeploymentApi.Editor;
namespace Unity.Services.Cli.Authoring.Model;

/// <summary>
/// Contain the data summary of a fetch operation.
/// </summary>
[Serializable]
public class FetchResult : AuthorResult
{
    public IReadOnlyList<IDeploymentItem> Fetched => Authored;

    public FetchResult(
        IReadOnlyList<IDeploymentItem> updated,
        IReadOnlyList<IDeploymentItem> deleted,
        IReadOnlyList<IDeploymentItem> created,
        IReadOnlyList<IDeploymentItem> authored,
        IReadOnlyList<IDeploymentItem> failed,
        bool dryRun = false)
        : base(updated, deleted, created, authored, failed, dryRun)
    {
    }

    public FetchResult(IReadOnlyList<AuthorResult> results, bool dryRun = false) : base(results, dryRun)
    {
    }

    internal override string Operation => "fetch";
}
