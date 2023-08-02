using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class ServersOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Servers = new List<Server>
        {
            new(
                id: ValidServerId,
                ip: "0.0.0.0",
                port: 9000,
                machineID: ValidMachineId,
                machineName: "test machine",
                machineSpec: new MachineSpec1("2020-12-31T12:00:00Z", "2020-01-01T12:00:00Z", "test-cpu"),
                locationID: ValidLocationId,
                locationName: ValidLocationName,
                fleetID: new Guid(ValidFleetId),
                fleetName: ValidFleetName,
                buildConfigurationID: ValidBuildConfigurationId,
                buildConfigurationName: ValidBuildConfigurationName,
                buildName: ValidBuildName,
                deleted: false
            )
        };
    }

    List<Server>? m_Servers;

    [Test]
    public void ConstructServersOutputWithValidInput()
    {
        ServersOutput output = new(m_Servers!);
        Assert.That(output, Has.Count.EqualTo(m_Servers!.Count));
        for (var i = 0; i < output.Count; i++)
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(output[i].Id, Is.EqualTo(m_Servers[i].Id));
                    Assert.That(output[i].Ip, Is.EqualTo(m_Servers[i].Ip));
                    Assert.That(output[i].Port, Is.EqualTo(m_Servers[i].Port));
                    Assert.That(output[i].MachineId, Is.EqualTo(m_Servers[i].MachineID));
                    Assert.That(output[i].LocationId, Is.EqualTo(m_Servers[i].LocationID));
                    Assert.That(output[i].LocationName, Is.EqualTo(m_Servers[i].LocationName));
                    Assert.That(output[i].FleetId, Is.EqualTo(m_Servers[i].FleetID));
                    Assert.That(output[i].FleetName, Is.EqualTo(m_Servers[i].FleetName));
                    Assert.That(output[i].BuildConfigurationId, Is.EqualTo(m_Servers[i].BuildConfigurationID));
                    Assert.That(output[i].BuildConfigurationName, Is.EqualTo(m_Servers[i].BuildConfigurationName));
                    Assert.That(output[i].BuildName, Is.EqualTo(m_Servers[i].BuildName));
                    Assert.That(output[i].Deleted, Is.EqualTo(m_Servers[i].Deleted));
                }
            );
        }
    }

    [Test]
    public void ServersOutputToString()
    {
        ServersOutput output = new(m_Servers!);
        var sb = new StringBuilder();
        foreach (var server in output)
        {
            sb.AppendLine($"- id: {server.Id}");
            sb.AppendLine("  ip: 0.0.0.0");
            sb.AppendLine("  port: 9000");
            sb.AppendLine($"  machineId: {server.MachineId}");
            sb.AppendLine($"  locationName: {server.LocationName}");
            sb.AppendLine($"  locationId: {server.LocationId}");
            sb.AppendLine($"  fleetName: {server.FleetName}");
            sb.AppendLine($"  fleetId: {server.FleetId}");
            sb.AppendLine($"  buildConfigurationName: {server.BuildConfigurationName}");
            sb.AppendLine($"  buildConfigurationId: {server.BuildConfigurationId}");
            sb.AppendLine($"  buildName: {server.BuildName}");
            sb.AppendLine("  deleted: false");
        }

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
