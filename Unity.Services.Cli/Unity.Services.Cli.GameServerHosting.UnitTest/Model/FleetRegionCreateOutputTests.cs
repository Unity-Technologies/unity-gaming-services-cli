using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class FleetRegionCreateOutputTests
{
    [SetUp]
    public void Setup()
    {
        m_FleetRegion = new NewFleetRegion(
            id: Guid.Parse(ValidFleetRegionId),
            maxServers: 2,
            minAvailableServers: 1,
            regionID: Guid.Parse(ValidRegionId),
            regionName: "RegionName"
        );
    }

    NewFleetRegion? m_FleetRegion;

    [Test]
    public void ConstructFleetRegionCreateOutputWithValidFleetRegion()
    {
        FleetRegionCreateOutput output = new(m_FleetRegion!);
        Assert.Multiple(() =>
        {
            Assert.That(output.FleetRegionId, Is.EqualTo(m_FleetRegion?.Id));
            Assert.That(output.MaxServers, Is.EqualTo(m_FleetRegion?.MaxServers));
            Assert.That(output.MinAvailableServers, Is.EqualTo(m_FleetRegion?.MinAvailableServers));
            Assert.That(output.RegionId, Is.EqualTo(m_FleetRegion?.RegionID));
            Assert.That(output.RegionName, Is.EqualTo(m_FleetRegion?.RegionName));
        });
    }

    [Test]
    public void FleetRegionCreateOutputToString()
    {
        var sb = new StringBuilder();
        FleetRegionCreateOutput output = new(m_FleetRegion!);

        sb.AppendLine($"fleetRegionId: {output.FleetRegionId}");
        sb.AppendLine($"maxServers: {output.MaxServers}");
        sb.AppendLine($"minAvailableServers: {output.MinAvailableServers}");
        sb.AppendLine($"regionId: {output.RegionId}");
        sb.AppendLine($"regionName: {output.RegionName}");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
