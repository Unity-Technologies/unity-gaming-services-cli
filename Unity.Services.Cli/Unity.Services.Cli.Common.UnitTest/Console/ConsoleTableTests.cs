using Moq;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Rendering;
using Unity.Services.Cli.Common.Console;

namespace Unity.Services.Cli.Common.UnitTest.Console;

[TestFixture]
public class ConsoleTableTests
{
    Mock<IAnsiConsole>? m_TestConsole;
    ConsoleTable? m_Table;

    [SetUp]
    public void SetUp()
    {
        m_TestConsole = new();
        m_Table = new(m_TestConsole.Object, false, false);
    }

    [Test]
    public void DrawingTableCallsSpectreConsoleWrite()
    {
        m_Table!.DrawTable();
        m_TestConsole!.Verify(c => c.Write(It.IsAny<Renderable>()));
    }

    [Test]
    public void TableAddMultipleColumnsAddsColumns()
    {
        m_Table!.AddColumns(new Text(""), new Text(""));
        Assert.AreEqual(2, m_Table.GetColumns().Count);
    }
}
