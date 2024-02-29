using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

public class RemoteConfigFetchResult : FetchResult
{
    public RemoteConfigFetchResult(
        IReadOnlyList<IDeploymentItem> updated,
        IReadOnlyList<IDeploymentItem> deleted,
        IReadOnlyList<IDeploymentItem> created,
        IReadOnlyList<IDeploymentItem> authored,
        IReadOnlyList<IDeploymentItem> failed,
        bool dryRun = false) :
        base(
            updated,
            deleted,
            created,
            authored,
            failed,
            dryRun)
    { }

    public override TableContent ToTable(string service = "")
    {
        var baseTable = new TableContent()
        {
            IsDryRun = DryRun
        };

        foreach (var file in Authored)
        {
            baseTable.AddRow(RowContent.ToRow(file));

            baseTable.AddRows(Updated.Where(key => key.Path == file.Path).Select(RowContent.ToRow).ToList());
            baseTable.AddRows(Deleted.Where(key => key.Path == file.Path).Select(RowContent.ToRow).ToList());
            baseTable.AddRows(Created.Where(key => key.Path == file.Path).Select(RowContent.ToRow).ToList());
            baseTable.AddRows(Failed.Where(key => key.Path == file.Path).Select(RowContent.ToRow).ToList());
        }

        baseTable.UpdateOrAddRows(Updated.Select(RowContent.ToRow).ToList());
        baseTable.UpdateOrAddRows(Deleted.Select(RowContent.ToRow).ToList());
        baseTable.UpdateOrAddRows(Created.Select(RowContent.ToRow).ToList());
        baseTable.UpdateOrAddRows(Failed.Select(RowContent.ToRow).ToList());

        return baseTable;
    }
}
