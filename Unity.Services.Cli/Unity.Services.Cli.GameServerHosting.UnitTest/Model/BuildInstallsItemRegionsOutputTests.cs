using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class BuildInstallsItemRegionsOutputTests
{
    [SetUp]
    public void Setup()
    {
        m_Regions = new List<RegionsInner>
        {
            new(
                1,
                2,
                3,
                "regionName"
            )
        };
    }

    List<RegionsInner>? m_Regions;

    [Test]
    public void ConstructBuildInstallsItemRegionsOutputWithValidRegions()
    {
        BuildInstallsItemRegionsOutput output = new(m_Regions!);
        Assert.That(output, Has.Count.EqualTo(m_Regions!.Count));
        for (var i = 0; i < output.Count; i++)
            Assert.Multiple(() =>
            {
                Assert.That(output[i].RegionName, Is.EqualTo(m_Regions[i].RegionName));
                Assert.That(output[i].PendingMachines, Is.EqualTo(m_Regions[i].PendingMachines));
                Assert.That(output[i].CompletedMachines, Is.EqualTo(m_Regions[i].CompletedMachines));
                Assert.That(output[i].Failures, Is.EqualTo(m_Regions[i].Failures));
            });
    }

    [Test]
    public void BuildInstallsItemRegionsOutputToString()
    {
        var sb = new StringBuilder();
        BuildInstallsItemRegionsOutput output = new(m_Regions!);
        foreach (var region in output)
        {
            sb.AppendLine($"- regionName: {region.RegionName}");
            sb.AppendLine($"  pendingMachines: {region.PendingMachines}");
            sb.AppendLine($"  completedMachines: {region.CompletedMachines}");
            sb.AppendLine($"  failures: {region.Failures}");
        }

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
