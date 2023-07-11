using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class FleetListItemOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Fleet = new FleetListItem(
            allocationType: FleetListItem.AllocationTypeEnum.ALLOCATION,
            new List<BuildConfiguration1>(),
            regions: new List<FleetRegion>(),
            id: new Guid(ValidFleetId),
            name: ValidFleetName,
            osName: OsNameLinux,
            servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus()),
                new FleetServerBreakdown(new ServerStatus())),
            status: FleetListItem.StatusEnum.ONLINE
        );
    }

    FleetListItem? m_Fleet;

    [Test]
    public void ConstructFleetListItemOutputWithValidList()
    {
        FleetListItemOutput output = new(m_Fleet!);
        Assert.Multiple(() =>
        {
            Assert.That(output.BuildConfigurations, Is.EqualTo(m_Fleet!.BuildConfigurations));
            Assert.That(output.Regions, Is.EqualTo(m_Fleet!.Regions));
            Assert.That(output.Id, Is.EqualTo(m_Fleet!.Id));
            Assert.That(output.Name, Is.EqualTo(m_Fleet!.Name));
            Assert.That(output.OsName, Is.EqualTo(m_Fleet!.OsName));
            Assert.That(output.Servers, Is.EqualTo(m_Fleet!.Servers));
            Assert.That(output.Status, Is.EqualTo(m_Fleet!.Status));
        });
    }

    [Test]
    public void FleetListItemOutputToString()
    {
        FleetListItemOutput output = new(m_Fleet!);
        var sb = new StringBuilder();
        sb.AppendLine("name: " + ValidFleetName);
        sb.AppendLine("id: " + ValidFleetId);
        sb.AppendLine("osName: " + OsNameLinux);
        sb.AppendLine("status: " + FleetListItem.StatusEnum.ONLINE);
        sb.AppendLine("buildConfigurations: []");
        sb.AppendLine("regions: []");
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
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
