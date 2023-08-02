using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class ServersItemOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Server = new Server(
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
        );
    }

    Server? m_Server;

    [Test]
    public void ConstructServersItemOutputWithValidInput()
    {
        ServersItemOutput output = new(m_Server!);
        Assert.Multiple(
            () =>
            {
                Assert.That(output.Id, Is.EqualTo(m_Server!.Id));
                Assert.That(output.Ip, Is.EqualTo(m_Server!.Ip));
                Assert.That(output.Port, Is.EqualTo(m_Server!.Port));
                Assert.That(output.MachineId, Is.EqualTo(m_Server!.MachineID));
                Assert.That(output.LocationId, Is.EqualTo(m_Server!.LocationID));
                Assert.That(output.LocationName, Is.EqualTo(m_Server!.LocationName));
                Assert.That(output.FleetId, Is.EqualTo(m_Server!.FleetID));
                Assert.That(output.FleetName, Is.EqualTo(m_Server!.FleetName));
                Assert.That(output.BuildConfigurationId, Is.EqualTo(m_Server!.BuildConfigurationID));
                Assert.That(output.BuildConfigurationName, Is.EqualTo(m_Server!.BuildConfigurationName));
                Assert.That(output.BuildName, Is.EqualTo(m_Server!.BuildName));
                Assert.That(output.Deleted, Is.EqualTo(m_Server!.Deleted));
            }
        );
    }

    [Test]
    public void ServersItemOutputToString()
    {
        ServersItemOutput output = new(m_Server!);
        var sb = new StringBuilder();
        sb.AppendLine($"id: {ValidServerId}");
        sb.AppendLine("ip: 0.0.0.0");
        sb.AppendLine("port: 9000");
        sb.AppendLine($"machineId: {ValidMachineId}");
        sb.AppendLine($"locationName: {ValidLocationName}");
        sb.AppendLine($"locationId: {ValidLocationId}");
        sb.AppendLine($"fleetName: {ValidFleetName}");
        sb.AppendLine($"fleetId: {ValidFleetId}");
        sb.AppendLine($"buildConfigurationName: {ValidBuildConfigurationName}");
        sb.AppendLine($"buildConfigurationId: {ValidBuildConfigurationId}");
        sb.AppendLine($"buildName: {ValidBuildName}");
        sb.AppendLine("deleted: false");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
