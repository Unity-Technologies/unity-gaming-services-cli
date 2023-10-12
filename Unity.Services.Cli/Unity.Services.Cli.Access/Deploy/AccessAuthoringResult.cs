using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Access.Deploy;

class AccessDeploymentResult : DeploymentResult
{
    public AccessDeploymentResult(
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
            dryRun)
    {
    }

    public override TableContent ToTable()
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
            var file = (IProjectAccessFile)deploymentItem;
            table.AddRow(RowContent.ToRow(file));
            foreach (var statement in file.Statements)
                table.AddRow(RowContent.ToRow(statement));
        }

        foreach (var deleted in res.Deleted)
        {
            table.AddRow(RowContent.ToRow(deleted));
        }

        foreach (var deploymentItem in res.Failed)
        {
            var file = (IProjectAccessFile)deploymentItem;
            table.AddRow(RowContent.ToRow(file));
            foreach (var statement in file.Statements)
                table.AddRow(RowContent.ToRow(statement));
        }

        return table;
    }
}

class AccessFetchResult : FetchResult
{
    public AccessFetchResult(
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
        dryRun)
    {
    }

    public override TableContent ToTable()
    {
        return AccessDeploymentResult.AccessControlResToTable(this);
    }
}
