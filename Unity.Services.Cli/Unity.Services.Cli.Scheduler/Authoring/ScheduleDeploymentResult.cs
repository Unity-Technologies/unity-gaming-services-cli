using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Scheduler.Deploy;

class ScheduleDeploymentResult : DeploymentResult
{
    public ScheduleDeploymentResult(
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
    { }

    public override TableContent ToTable(string service = "")
    {
        return SchedulerResToTable(this, service);
    }

    public static TableContent SchedulerResToTable(AuthorResult res, string service)
    {
        var table = new TableContent
        {
            IsDryRun = res.DryRun
        };

        foreach (var deploymentItem in res.Authored)
        {
            table.AddRow(new RowContent(deploymentItem, service));

            foreach (var updated in res.Updated.Where(item => item.Path == deploymentItem.Path))
            {
                table.AddRow(new RowContent(updated, service));
            }

            foreach (var created in res.Created.Where(item => item.Path == deploymentItem.Path))
            {
                table.AddRow(new RowContent(created, service));
            }
        }

        foreach (var deleted in res.Deleted)
        {
            table.AddRow(new RowContent(deleted, service));
        }

        foreach (var deploymentItem in res.Failed)
        {
            table.AddRow(new RowContent(deploymentItem, service));
        }

        return table;
    }
}

public class SchedulesFetchResult : FetchResult
{
    public SchedulesFetchResult(
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
    { }

    public override TableContent ToTable(string service = "")
    {
        return ScheduleDeploymentResult.SchedulerResToTable(this, service);
    }
}
