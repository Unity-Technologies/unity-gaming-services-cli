using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.Cli.RemoteConfig.Model;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Model;

class CliRemoteConfigEntryTests
{
    const string k_EntryName = "test_name";
    const string k_EntryType = "test_type";
    const string k_EntryPath = "test_path";
    const float k_EntryProgress = 0f;
    const string k_EntryStatus = "tests_status";
    const string k_EntryDetail = "tests_status_details";

    readonly CliRemoteConfigEntry m_RemoteConfigEntry = new CliRemoteConfigEntry(
        k_EntryName,
        k_EntryType,
        k_EntryPath,
        k_EntryProgress,
        k_EntryStatus,
        k_EntryDetail);


    [Test]
    public void CliRemoteConfigEntryToRowWorksCorrectly()
    {
        var rowResult = RowContent.ToRow(m_RemoteConfigEntry);
        Assert.Multiple(() =>
        {
            Assert.That(rowResult.Name, Is.EqualTo(k_EntryName));
            Assert.That(rowResult.Type, Is.EqualTo(k_EntryType));
            Assert.That(rowResult.Status, Is.EqualTo(k_EntryStatus));
            Assert.That(rowResult.Details, Is.EqualTo(k_EntryDetail));
            Assert.That(rowResult.Path, Is.EqualTo(k_EntryPath));
        });
    }
}
