using Spectre.Console;

namespace Unity.Services.Cli.Targeting.Models;

public abstract class BaseTable<T>
{
    protected internal readonly Table Table;
    protected readonly List<T> Items = new();
    readonly bool m_IsQuiet;
    readonly IAnsiConsole m_Console;
    IPageRange? m_PageRange;

    protected abstract string EntityTypeName { get; }

    protected BaseTable(
        IAnsiConsole console,
        bool isQuiet = false,
        IPageRange? pageRange = null)
    {
        m_Console = console;
        m_IsQuiet = isQuiet;
        Table = new Table();
        m_PageRange = pageRange;
        SetupTable();
    }

    protected abstract void SetupTable();

    protected void AddItems(List<T> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    protected abstract void AddItem(T item);

    protected abstract string GetItemName(T item);

    protected static string SanitizeForTable(string? value)
    {
        return string.IsNullOrEmpty(value) ? "N/A" : value.Trim().EscapeMarkup();
    }

    public void Draw()
    {
        if (m_IsQuiet)
        {
            foreach (var item in Items)
            {
                m_Console.WriteLine(GetItemName(item));
            }
        }
        else
        {
            SetFooter();
            m_Console.Write(Table);
        }
    }

    void SetFooter()
    {
        if (m_PageRange == null)
            return;
        var footer =
            $"Showing {m_PageRange.StartIndex + 1}-{Math.Min(m_PageRange.EndIndex + 1, m_PageRange.Total)} of {m_PageRange.Total} {EntityTypeName}";
        Table.Caption(footer);
        Table.Caption(new TableTitle(footer));
    }
}
