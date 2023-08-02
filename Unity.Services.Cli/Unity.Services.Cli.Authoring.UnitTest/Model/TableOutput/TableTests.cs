using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model.TableOutput;

namespace Unity.Services.Cli.Authoring.UnitTest.Model.TableOutput;

public class TableTests
{
    TableContent m_Table = new TableContent();

    const string k_StartEntryResource = "Test";
    const string k_UpdatedEntryLocation = "NewDataValue";


    readonly RowContent m_StartTableRow = new RowContent(k_StartEntryResource);
    readonly RowContent m_UpdatedTableRow = new RowContent(k_StartEntryResource, k_UpdatedEntryLocation);

    [Test]
    public void UpdateRowsWorksCorrectly()
    {
        m_Table = new TableContent();

        m_Table.AddRow(m_StartTableRow);
        m_Table.UpdateOrAddRows(
            new[]
            {
                m_UpdatedTableRow
            });

        Assert.Contains(m_UpdatedTableRow, m_Table.Result.ToList());
    }

    [Test]
    public void AddRowWorksCorrectly()
    {
        m_Table = new TableContent();

        m_Table.AddRow(m_StartTableRow);

        Assert.Contains(m_StartTableRow, m_Table.Result.ToList());
    }

    [Test]
    public void AddRowsWorksCorrectly()
    {
        m_Table = new TableContent();

        var newTable = new TableContent();

        newTable.AddRow(m_StartTableRow);

        m_Table.AddRows(
            new[]
            {
                newTable
            });

        Assert.Contains(m_StartTableRow, m_Table.Result.ToList());
    }
}
