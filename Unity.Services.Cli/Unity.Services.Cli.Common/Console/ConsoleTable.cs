using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

class ConsoleTable : IConsoleTable
{
    readonly IAnsiConsole m_Console;
    public bool IsStandardInputRedirected { get; }
    internal readonly Table SpectreTable = new();
    readonly bool m_OutputIsJson;

    public ConsoleTable(
        IAnsiConsole console,
        bool isStandardInputRedirected,
        bool outputIsJson)
    {
        m_Console = console;
        IsStandardInputRedirected = isStandardInputRedirected;
        m_OutputIsJson = outputIsJson;

        // Define generic CLI Table style here
        SpectreTable.Border(TableBorder.Rounded);
    }

    public void DrawTable()
    {
        if (m_OutputIsJson)
        {
            return;
        }
        m_Console.Write(SpectreTable);
    }

    public void AddColumn(Text title)
        => SpectreTable.AddColumn(new TableColumn(title));

    public void AddColumns(params Text[] titles)
    {
        foreach (var title in titles)
        {
            AddColumn(title);
        }
    }

    public void AddRow(params Text[] items)
        => SpectreTable.AddRow(new TableRow(items));

    public void RemoveRow(int index)
        => SpectreTable.RemoveRow(index);

    public IReadOnlyList<TableColumn> GetColumns()
        => SpectreTable.Columns;

    public IReadOnlyList<TableRow> GetRows()
        => SpectreTable.Rows;

    public void SetTitle(TableTitle title)
        => SpectreTable.Title = title;
}
