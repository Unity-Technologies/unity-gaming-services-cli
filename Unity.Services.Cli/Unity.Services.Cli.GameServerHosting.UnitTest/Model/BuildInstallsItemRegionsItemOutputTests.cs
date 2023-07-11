using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildInstallsItemRegionsItemOutputTests
{
    [SetUp]
    public void Setup()
    {
        m_Region = new RegionsInner(
            1,
            2,
            3,
            "regionName"
        );
    }

    RegionsInner? m_Region;

    [Test]
    public void ConstructBuildInstallsItemRegionsItemOutputWithValidRegion()
    {
        BuildInstallsItemRegionsItemOutput output = new(m_Region!);
        Assert.Multiple(() =>
        {
            Assert.That(output.RegionName, Is.EqualTo(m_Region?.RegionName));
            Assert.That(output.PendingMachines, Is.EqualTo(m_Region?.PendingMachines));
            Assert.That(output.CompletedMachines, Is.EqualTo(m_Region?.CompletedMachines));
            Assert.That(output.Failures, Is.EqualTo(m_Region?.Failures));
        });
    }

    [Test]
    public void BuildInstallsItemRegionsItemOutputToString()
    {
        var sb = new StringBuilder();
        BuildInstallsItemRegionsItemOutput output = new(m_Region!);
        sb.AppendLine($"regionName: {output.RegionName}");
        sb.AppendLine($"pendingMachines: {output.PendingMachines}");
        sb.AppendLine($"completedMachines: {output.CompletedMachines}");
        sb.AppendLine($"failures: {output.Failures}");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
