using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class MachinesItemOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Machine = new Machine(
            id: ValidMachineId,
            ip: "127.0.0.10",
            name: ValidMachineName,
            locationId: ValidLocationId,
            locationName: ValidLocationName,
            fleetId: new Guid(ValidFleetId),
            fleetName: ValidFleetName,
            hardwareType: Machine.HardwareTypeEnum.CLOUD,
            osFamily: Machine.OsFamilyEnum.LINUX,
            osName: OsNameFullNameLinux,
            serversStates: new ServersStates(
                allocated: 1,
                available: 2,
                held: 3,
                online: 4,
                reserved: 5
            ),
            spec: new MachineSpec(
                cpuCores: 1,
                cpuShortname: ValidMachineCpuSeriesShortname,
                cpuSpeed: 1000,
                cpuType: ValidMachineCpuType,
                memory: 1000000
            ),
            status: Machine.StatusEnum.ONLINE,
            deleted: false,
            disabled: false
        );
    }

    Machine? m_Machine;

    [Test]
    public void ConstructMachinesItemOutputWithValidInput()
    {
        MachinesItemOutput output = new(m_Machine!);
        Assert.Multiple(
            () =>
            {
                Assert.That(output.Id, Is.EqualTo(m_Machine!.Id));
                Assert.That(output.Ip, Is.EqualTo(m_Machine!.Ip));
                Assert.That(output.Name, Is.EqualTo(m_Machine!.Name));
                Assert.That(output.LocationId, Is.EqualTo(m_Machine!.LocationId));
                Assert.That(output.LocationName, Is.EqualTo(m_Machine!.LocationName));
                Assert.That(output.FleetId, Is.EqualTo(m_Machine!.FleetId));
                Assert.That(output.FleetName, Is.EqualTo(m_Machine!.FleetName));
                Assert.That(output.HardwareType, Is.EqualTo(m_Machine!.HardwareType));
                Assert.That(output.OsFamily, Is.EqualTo(m_Machine!.OsFamily));
                Assert.That(output.OsName, Is.EqualTo(m_Machine!.OsName));
                Assert.That(output.ServersStates.Allocated, Is.EqualTo(m_Machine!.ServersStates.Allocated));
                Assert.That(output.ServersStates.Available, Is.EqualTo(m_Machine!.ServersStates.Available));
                Assert.That(output.ServersStates.Held, Is.EqualTo(m_Machine!.ServersStates.Held));
                Assert.That(output.ServersStates.Online, Is.EqualTo(m_Machine!.ServersStates.Online));
                Assert.That(output.ServersStates.Reserved, Is.EqualTo(m_Machine!.ServersStates.Reserved));
                Assert.That(output.Spec.CpuCores, Is.EqualTo(m_Machine!.Spec.CpuCores));
                Assert.That(output.Spec.CpuShortname, Is.EqualTo(m_Machine!.Spec.CpuShortname));
                Assert.That(output.Spec.CpuSpeed, Is.EqualTo(m_Machine!.Spec.CpuSpeed));
                Assert.That(output.Spec.CpuType, Is.EqualTo(m_Machine!.Spec.CpuType));
                Assert.That(output.Spec.Memory, Is.EqualTo(m_Machine!.Spec.Memory));
                Assert.That(output.Status, Is.EqualTo(m_Machine!.Status));
                Assert.That(output.Deleted, Is.EqualTo(m_Machine!.Deleted));
                Assert.That(output.Disabled, Is.EqualTo(m_Machine!.Disabled));
            }
        );
    }

    [Test]
    public void MachineItemOutputToString()
    {
        MachinesItemOutput output = new(m_Machine!);
        var sb = new StringBuilder();
        sb.AppendLine($"id: {m_Machine!.Id}");
        sb.AppendLine($"ip: {m_Machine!.Ip}");
        sb.AppendLine($"name: {m_Machine!.Name}");
        sb.AppendLine($"locationName: {m_Machine!.LocationName}");
        sb.AppendLine($"locationId: {m_Machine!.LocationId}");
        sb.AppendLine($"fleetName: {m_Machine!.FleetName}");
        sb.AppendLine($"fleetId: {m_Machine!.FleetId}");
        sb.AppendLine($"hardwareType: {m_Machine!.HardwareType}");
        sb.AppendLine($"osFamily: {m_Machine!.OsFamily}");
        sb.AppendLine($"osName: {m_Machine!.OsName}");
        sb.AppendLine($"serversStates:");
        sb.AppendLine($"  allocated: {m_Machine!.ServersStates.Allocated}");
        sb.AppendLine($"  available: {m_Machine!.ServersStates.Available}");
        sb.AppendLine($"  held: {m_Machine!.ServersStates.Held}");
        sb.AppendLine($"  online: {m_Machine!.ServersStates.Online}");
        sb.AppendLine($"  reserved: {m_Machine!.ServersStates.Reserved}");
        sb.AppendLine($"spec:");
        sb.AppendLine($"  cpuCores: {m_Machine!.Spec.CpuCores}");
        sb.AppendLine($"  cpuShortname: {m_Machine!.Spec.CpuShortname}");
        sb.AppendLine($"  cpuSpeed: {m_Machine!.Spec.CpuSpeed}");
        sb.AppendLine($"  cpuType: {m_Machine!.Spec.CpuType}");
        sb.AppendLine($"  memory: {m_Machine!.Spec.Memory}");
        sb.AppendLine($"status: {m_Machine!.Status}");
        sb.AppendLine("deleted: false");
        sb.AppendLine("disabled: false");

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
