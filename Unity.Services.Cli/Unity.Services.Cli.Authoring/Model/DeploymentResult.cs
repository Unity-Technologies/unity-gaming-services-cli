using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.Model;

[Serializable]
public class DeploymentResult : AuthorResult
{
    public IReadOnlyList<IDeploymentItem> Deployed => Authored;

    public DeploymentResult(
        IReadOnlyList<IDeploymentItem> updated,
        IReadOnlyList<IDeploymentItem> deleted,
        IReadOnlyList<IDeploymentItem> created,
        IReadOnlyList<IDeploymentItem> authored,
        IReadOnlyList<IDeploymentItem> failed,
        bool dryRun = false)
        : base(updated, deleted, created, authored, failed, dryRun) { }

    public DeploymentResult(IReadOnlyList<AuthorResult> results, bool dryRun = false) : base(results, dryRun) { }

    public DeploymentResult(IReadOnlyList<IDeploymentItem> results) : base(results) { }

    internal override string Operation => "deploy";
}
