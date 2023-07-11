using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class FleetGetOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Fleet = new Fleet(
            buildConfigurations: new List<BuildConfiguration2>(),
            fleetRegions: new List<FleetRegion1>(),
            id: new Guid(ValidFleetId),
            name: ValidFleetName,
            osFamily: Fleet.OsFamilyEnum.LINUX,
            osName: OsNameLinux,
            servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus()), new FleetServerBreakdown(new ServerStatus())),
            status: Fleet.StatusEnum.ONLINE,
            allocationTTL: 10,
            deleteTTL: 20,
            disabledDeleteTTL: 25,
            shutdownTTL: 30
        );
    }

    Fleet? m_Fleet;

    [Test]
    public void ConstructFleetGetOutputWithValidFleet()
    {
        FleetGetOutput output = new(m_Fleet!);
        Assert.Multiple(() =>
        {
            Assert.That(output.Name, Is.EqualTo(m_Fleet!.Name));
            Assert.That(output.Id, Is.EqualTo(m_Fleet!.Id));
            Assert.That(output.OsFamily, Is.EqualTo(m_Fleet!.OsFamily));
            Assert.That(output.OsName, Is.EqualTo(m_Fleet!.OsName));
            Assert.That(output.Status, Is.EqualTo(m_Fleet!.Status));
            Assert.That(output.BuildConfigurations, Is.EqualTo(m_Fleet!.BuildConfigurations));
            Assert.That(output.FleetRegions, Is.EqualTo(m_Fleet!.FleetRegions));
            Assert.That(output.Servers, Is.EqualTo(m_Fleet!.Servers));
            Assert.That(output.AllocationTtl, Is.EqualTo(m_Fleet!.AllocationTTL));
            Assert.That(output.DeleteTtl, Is.EqualTo(m_Fleet!.DeleteTTL));
            Assert.That(output.DisabledDeleteTtl, Is.EqualTo(m_Fleet!.DisabledDeleteTTL));
            Assert.That(output.ShutdownTtl, Is.EqualTo(m_Fleet!.ShutdownTTL));
        });
    }

    [Test]
    public void FleetGetOutputToString()
    {
        FleetGetOutput output = new(m_Fleet!);
        var sb = new StringBuilder();
        sb.AppendLine("name: Fleet One");
        sb.AppendLine("id: 00000000-0000-0000-1000-000000000000");
        sb.AppendLine("osFamily: LINUX");
        sb.AppendLine("osName: Linux");
        sb.AppendLine("status: ONLINE");
        sb.AppendLine("buildConfigurations: []");
        sb.AppendLine("fleetRegions: []");
        sb.AppendLine("servers:");
        sb.AppendLine("  all:");
        sb.AppendLine("    status:");
        sb.AppendLine("      allocated: 0");
        sb.AppendLine("      available: 0");
        sb.AppendLine("      online: 0");
        sb.AppendLine("    total: 0");
        sb.AppendLine("  cloud:");
        sb.AppendLine("    status:");
        sb.AppendLine("      allocated: 0");
        sb.AppendLine("      available: 0");
        sb.AppendLine("      online: 0");
        sb.AppendLine("    total: 0");
        sb.AppendLine("  metal:");
        sb.AppendLine("    status:");
        sb.AppendLine("      allocated: 0");
        sb.AppendLine("      available: 0");
        sb.AppendLine("      online: 0");
        sb.AppendLine("    total: 0");
        sb.AppendLine("allocationTtl: 10");
        sb.AppendLine("deleteTtl: 20");
        sb.AppendLine("disabledDeleteTtl: 25");
        sb.AppendLine("shutdownTtl: 30");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
