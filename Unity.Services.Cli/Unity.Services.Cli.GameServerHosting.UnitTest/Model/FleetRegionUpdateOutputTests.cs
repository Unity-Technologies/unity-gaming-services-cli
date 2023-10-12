using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class FleetRegionUpdateOutputTests
{
    [SetUp]
    public void Setup()
    {
        m_FleetRegion = new UpdatedFleetRegion(
            deleteTTL: 120,
            disabledDeleteTTL: 60,
            id: Guid.Parse(ValidFleetRegionId),
            maxServers: 3,
            minAvailableServers: 3,
            regionID: Guid.Parse(ValidRegionId),
            regionName: "RegionName",
            scalingEnabled: true,
            shutdownTTL: 180
        );
    }

    UpdatedFleetRegion? m_FleetRegion;

    [Test]
    public void ConstructFleetRegionUpdateOutputWithValidFleetRegion()
    {
        FleetRegionUpdateOutput output = new(m_FleetRegion!);
        Assert.Multiple(() =>
        {
            Assert.That(output.DeleteTtl, Is.EqualTo(m_FleetRegion?.DeleteTTL));
            Assert.That(output.DisabledDeleteTtl, Is.EqualTo(m_FleetRegion?.DisabledDeleteTTL));
            Assert.That(output.Id, Is.EqualTo(m_FleetRegion?.Id));
            Assert.That(output.MaxServers, Is.EqualTo(m_FleetRegion?.MaxServers));
            Assert.That(output.MinAvailableServers, Is.EqualTo(m_FleetRegion?.MinAvailableServers));
            Assert.That(output.RegionId, Is.EqualTo(m_FleetRegion?.RegionID));
            Assert.That(output.RegionName, Is.EqualTo(m_FleetRegion?.RegionName));
            Assert.That(output.ScalingEnabled, Is.EqualTo(m_FleetRegion?.ScalingEnabled));
            Assert.That(output.ShutdownTtl, Is.EqualTo(m_FleetRegion?.ShutdownTTL));
        }
        );
    }

    [Test]
    public void FleetRegionUpdateOutputToString()
    {
        var sb = new StringBuilder();
        FleetRegionUpdateOutput output = new(m_FleetRegion!);

        sb.AppendLine($"deleteTtl: {output.DeleteTtl}");
        sb.AppendLine($"disabledDeleteTtl: {output.DisabledDeleteTtl}");
        sb.AppendLine($"id: {output.Id}");
        sb.AppendLine($"maxServers: {output.MaxServers}");
        sb.AppendLine($"minAvailableServers: {output.MinAvailableServers}");
        sb.AppendLine($"regionId: {output.RegionId}");
        sb.AppendLine($"regionName: {output.RegionName}");
        sb.AppendLine($"scalingEnabled: {output.ScalingEnabled.ToString().ToLower()}");
        sb.AppendLine($"shutdownTtl: {output.ShutdownTtl}");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
