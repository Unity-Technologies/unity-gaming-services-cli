using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.Model.TableOutput;

[Serializable]
public class TableContent
{
    public bool IsDryRun { get; set; }
    public List<RowContent> Result { get; protected set; }

    public TableContent()
    {
        Result = new List<RowContent>();
    }

    public TableContent(IReadOnlyList<RowContent> rows)
    {
        Result = rows.ToList();
    }

    public void AddRows(IReadOnlyList<TableContent> tables)
    {
        foreach (var item in tables)
        {
            AddRows(item.Result);
        }
    }

    public void AddRows(TableContent table)
    {
        AddRows(table.Result);
    }

    public void AddRows(IReadOnlyList<RowContent> rows)
    {
        Result.AddRange(rows);
    }

    public void AddRow(RowContent row)
    {
        Result.Add(row);
    }

    public void UpdateOrAddRows(IReadOnlyList<RowContent> items)
    {
        foreach (var item in items)
        {
            UpdateOrAddRow(item);
        }
    }

    void UpdateOrAddRow(RowContent item)
    {
        var index = Result.FindIndex(row => row.Name == item.Name);

        if (index != -1)
        {
            Result[index] = item;
        }
        else
        {
            AddRow(item);
        }
    }

    public static TableContent ToTable(IDeploymentItem item)
    {
        return new TableContent(
            new[]
            {
                RowContent.ToRow(item)
            });
    }
}
