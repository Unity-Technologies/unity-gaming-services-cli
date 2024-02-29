using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Triggers.Deploy;

class TriggersDeploymentResult : DeploymentResult
{
    public TriggersDeploymentResult(
        IReadOnlyList<IDeploymentItem> updated,
        IReadOnlyList<IDeploymentItem> deleted,
        IReadOnlyList<IDeploymentItem> created,
        IReadOnlyList<IDeploymentItem> authored,
        IReadOnlyList<IDeploymentItem> failed,
        bool dryRun = false)
        : base(
            updated,
            deleted,
            created,
            authored,
            failed,
            dryRun) { }

    public override TableContent ToTable(string service = "")
    {
        return AccessControlResToTable(this);
    }

    public static TableContent AccessControlResToTable(AuthorResult res)
    {
        var table = new TableContent
        {
            IsDryRun = res.DryRun
        };

        foreach (var deploymentItem in res.Authored)
        {
            var file = (TriggersFileItem)deploymentItem;
            table.AddRow(RowContent.ToRow(file));
            foreach (var statement in file.Content.Configs)
                table.AddRow(RowContent.ToRow(statement));
        }

        foreach (var deleted in res.Deleted)
        {
            table.AddRow(RowContent.ToRow(deleted));
        }

        foreach (var deploymentItem in res.Failed)
        {
            var file = (TriggersFileItem)deploymentItem;
            table.AddRow(RowContent.ToRow(file));
            foreach (var statement in file.Content.Configs)
                table.AddRow(RowContent.ToRow(statement));
        }

        return table;
    }
}

public class TriggersFetchResult : FetchResult
{
    public TriggersFetchResult(
        IReadOnlyList<IDeploymentItem> updated,
        IReadOnlyList<IDeploymentItem> deleted,
        IReadOnlyList<IDeploymentItem> created,
        IReadOnlyList<IDeploymentItem> authored,
        IReadOnlyList<IDeploymentItem> failed,
        bool dryRun = false) : base(
        updated,
        deleted,
        created,
        authored,
        failed,
        dryRun) { }

    public override TableContent ToTable(string service = "")
    {
        return TriggersDeploymentResult.AccessControlResToTable(this);
    }
}
