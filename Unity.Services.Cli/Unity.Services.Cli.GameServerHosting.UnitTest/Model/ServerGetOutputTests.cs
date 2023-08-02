using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class ServerGetOutputTests
{
    Server? m_Server;


    [SetUp]
    public void SetUp()
    {
        m_Server = new Server(
                buildConfigurationID: ValidBuildConfigurationId,
                buildConfigurationName: "buildConfigurationName",
                buildName: ValidBuildName,
                deleted: false,
                fleetID: new Guid(ValidFleetId),
                fleetName: "fleetName",
                hardwareType: Server.HardwareTypeEnum.METAL,
                id: 1,
                ip: "192.168.1.1",
                locationID: 3,
                locationName: "locationName",
                machineName: "test machine",
                machineSpec: new MachineSpec1("2020-12-31T12:00:00Z", "2020-01-01T12:00:00Z", "test-cpu"),
                machineID: 5,
                port: 440,
                status: Server.StatusEnum.READY
            );
    }

    [Test]
    public void ConstructServerGetOutput()
    {
        ServerGetOutput output = new ServerGetOutput(m_Server!);
        Assert.Multiple(() =>
        {
            Assert.That(output.BuildConfigurationId, Is.EqualTo(m_Server!.BuildConfigurationID));
            Assert.That(output.BuildConfigurationName, Is.EqualTo(m_Server!.BuildConfigurationName));
            Assert.That(output.BuildName, Is.EqualTo(m_Server!.BuildName));
            Assert.That(output.Deleted, Is.EqualTo(m_Server!.Deleted));
            Assert.That(output.FleetId, Is.EqualTo(m_Server!.FleetID));
            Assert.That(output.FleetName, Is.EqualTo(m_Server!.FleetName));
            Assert.That(output.HardwareType, Is.EqualTo(m_Server!.HardwareType));
            Assert.That(output.Id, Is.EqualTo(m_Server!.Id));
            Assert.That(output.Ip, Is.EqualTo(m_Server!.Ip));
            Assert.That(output.LocationId, Is.EqualTo(m_Server!.LocationID));
            Assert.That(output.LocationName, Is.EqualTo(m_Server!.LocationName));
            Assert.That(output.MachineId, Is.EqualTo(m_Server!.MachineID));
            Assert.That(output.Port, Is.EqualTo(m_Server!.Port));
            Assert.That(output.Status, Is.EqualTo(m_Server!.Status));
        });

    }

    [Test]
    public void ServerGetOutputToString()
    {
        ServerGetOutput output = new(m_Server!);
        var sb = new StringBuilder();
        sb.AppendLine("buildConfigurationId: 1");
        sb.AppendLine("buildConfigurationName: buildConfigurationName");
        sb.AppendLine("buildName: Build One");
        sb.AppendLine("deleted: false");
        sb.AppendLine("fleetId: 00000000-0000-0000-1000-000000000000");
        sb.AppendLine("fleetName: fleetName");
        sb.AppendLine("hardwareType: METAL");
        sb.AppendLine("id: 1");
        sb.AppendLine("ip: 192.168.1.1");
        sb.AppendLine("locationId: 3");
        sb.AppendLine("locationName: locationName");
        sb.AppendLine("machineId: 5");
        sb.AppendLine("port: 440");
        sb.AppendLine("status: READY");
        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
