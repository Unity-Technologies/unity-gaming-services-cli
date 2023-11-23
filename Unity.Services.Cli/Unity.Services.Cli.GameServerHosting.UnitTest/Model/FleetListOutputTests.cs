using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
class FleetListOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Fleets = new List<FleetListItem>
        {
            new(
                allocationType: FleetListItem.AllocationTypeEnum.ALLOCATION,
                new List<BuildConfiguration1>(),
                regions: new List<FleetRegion>(),
                id: new Guid(ValidFleetId),
                name: ValidFleetName,
                osName: OsNameLinux,
                servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus())),
                status: FleetListItem.StatusEnum.ONLINE,
                usageSettings: new List<FleetUsageSetting>()
            ),
            new(
                allocationType: FleetListItem.AllocationTypeEnum.ALLOCATION,
                new List<BuildConfiguration1>(),
                regions: new List<FleetRegion>(),
                id: new Guid(ValidFleetId2),
                name: ValidFleetName2,
                osName: OsNameLinux,
                servers: new Servers(new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus()),
                    new FleetServerBreakdown(new ServerStatus())),
                status: FleetListItem.StatusEnum.ONLINE,
                usageSettings: new List<FleetUsageSetting>()
            )
        };
    }

    List<FleetListItem>? m_Fleets;

    [Test]
    public void ConstructFleetListOutputWithValidList()
    {
        FleetListOutput output = new(m_Fleets!);
        Assert.That(output, Has.Count.EqualTo(m_Fleets!.Count));
        for (var i = 0; i < output.Count; i++)
            Assert.Multiple(() =>
            {
                Assert.That(output[i].BuildConfigurations, Is.EqualTo(m_Fleets[i].BuildConfigurations));
                Assert.That(output[i].Regions, Is.EqualTo(m_Fleets[i].Regions));
                Assert.That(output[i].Id, Is.EqualTo(m_Fleets[i].Id));
                Assert.That(output[i].Name, Is.EqualTo(m_Fleets[i].Name));
                Assert.That(output[i].OsName, Is.EqualTo(m_Fleets[i].OsName));
                Assert.That(output[i].Servers, Is.EqualTo(m_Fleets[i].Servers));
                Assert.That(output[i].Status, Is.EqualTo(m_Fleets[i].Status));
                Assert.That(output[i].UsageSettings, Is.EqualTo(m_Fleets[i].UsageSettings));
            });
    }

    [Test]
    public void FleetListOutputToString()
    {
        FleetListOutput output = new(m_Fleets!);
        var sb = new StringBuilder();
        foreach (var fleet in output)
        {
            sb.AppendLine($"- name: {fleet.Name}");
            sb.AppendLine($"  id: {fleet.Id}");
            sb.AppendLine($"  osName: {fleet.OsName}");
            sb.AppendLine($"  status: {fleet.Status}");
            sb.AppendLine("  buildConfigurations: []");
            sb.AppendLine("  regions: []");
            sb.AppendLine("  servers:");
            sb.AppendLine("    all:");
            sb.AppendLine("      status:");
            sb.AppendLine($"        allocated: {fleet.Servers.All.Status.Allocated}");
            sb.AppendLine($"        available: {fleet.Servers.All.Status.Available}");
            sb.AppendLine($"        online: {fleet.Servers.All.Status.Online}");
            sb.AppendLine($"      total: {fleet.Servers.All.Total}");
            sb.AppendLine("    cloud:");
            sb.AppendLine("      status:");
            sb.AppendLine($"        allocated: {fleet.Servers.Cloud.Status.Allocated}");
            sb.AppendLine($"        available: {fleet.Servers.Cloud.Status.Available}");
            sb.AppendLine($"        online: {fleet.Servers.Cloud.Status.Online}");
            sb.AppendLine($"      total: {fleet.Servers.Cloud.Total}");
            sb.AppendLine("    metal:");
            sb.AppendLine("      status:");
            sb.AppendLine($"        allocated: {fleet.Servers.Metal.Status.Allocated}");
            sb.AppendLine($"        available: {fleet.Servers.Metal.Status.Available}");
            sb.AppendLine($"        online: {fleet.Servers.Metal.Status.Online}");
            sb.AppendLine($"      total: {fleet.Servers.Metal.Total}");
            sb.AppendLine("  usageSettings: []");
        }

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
