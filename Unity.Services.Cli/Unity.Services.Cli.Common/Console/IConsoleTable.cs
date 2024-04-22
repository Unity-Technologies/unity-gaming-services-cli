using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

public interface IConsoleTable
{
    void DrawTable();
    void AddColumn(Text title);
    void AddColumns(params Text[] titles);
    void AddRow(params Text[] items);
    void RemoveRow(int index);
    IReadOnlyList<TableColumn> GetColumns();
    IReadOnlyList<TableRow> GetRows();
    void SetTitle(TableTitle title);
}
