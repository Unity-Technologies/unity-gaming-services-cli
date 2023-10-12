using System.Text;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Model;

[TestFixture]
public class MachinesOutputTests
{
    [SetUp]
    public void SetUp()
    {
        m_Machines = new List<Machine>
        {
            new (
                id: ValidMachineId,
                ip: "127.0.0.1",
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
                    online: 3,
                    reserved: 4,
                    held: 5
                ),
                spec: new MachineSpec(
                    cpuCores: 1,
                    cpuShortname:  ValidMachineCpuSeriesShortname,
                    cpuSpeed: 1000,
                    cpuType:  ValidMachineCpuType,
                    memory:100000
                ),
                status: Machine.StatusEnum.ONLINE,
                deleted: false,
                disabled: false
            )
        };
    }

    List<Machine>? m_Machines;

    [Test]
    public void ConstructMachinesOutputWithValidInput()
    {
        MachinesOutput output = new(m_Machines!);
        Assert.That(output, Has.Count.EqualTo(m_Machines!.Count));
        for (var i = 0; i < output.Count; i++)
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(output[i].Id, Is.EqualTo(m_Machines[i].Id));
                    Assert.That(output[i].Ip, Is.EqualTo(m_Machines[i].Ip));
                    Assert.That(output[i].Name, Is.EqualTo(m_Machines[i].Name));
                    Assert.That(output[i].LocationId, Is.EqualTo(m_Machines[i].LocationId));
                    Assert.That(output[i].LocationName, Is.EqualTo(m_Machines[i].LocationName));
                    Assert.That(output[i].FleetId, Is.EqualTo(m_Machines[i].FleetId));
                    Assert.That(output[i].FleetName, Is.EqualTo(m_Machines[i].FleetName));
                    Assert.That(output[i].HardwareType, Is.EqualTo(m_Machines[i].HardwareType));
                    Assert.That(output[i].OsFamily, Is.EqualTo(m_Machines[i].OsFamily));
                    Assert.That(output[i].OsName, Is.EqualTo(m_Machines[i].OsName));
                    Assert.That(output[i].ServersStates.Allocated, Is.EqualTo(m_Machines[i].ServersStates.Allocated));
                    Assert.That(output[i].ServersStates.Available, Is.EqualTo(m_Machines[i].ServersStates.Available));
                    Assert.That(output[i].ServersStates.Held, Is.EqualTo(m_Machines[i].ServersStates.Held));
                    Assert.That(output[i].ServersStates.Online, Is.EqualTo(m_Machines[i].ServersStates.Online));
                    Assert.That(output[i].ServersStates.Reserved, Is.EqualTo(m_Machines[i].ServersStates.Reserved));
                    Assert.That(output[i].Spec.CpuCores, Is.EqualTo(m_Machines[i].Spec.CpuCores));
                    Assert.That(output[i].Spec.CpuShortname, Is.EqualTo(m_Machines[i].Spec.CpuShortname));
                    Assert.That(output[i].Spec.CpuSpeed, Is.EqualTo(m_Machines[i].Spec.CpuSpeed));
                    Assert.That(output[i].Spec.CpuType, Is.EqualTo(m_Machines[i].Spec.CpuType));
                    Assert.That(output[i].Spec.Memory, Is.EqualTo(m_Machines[i].Spec.Memory));
                    Assert.That(output[i].Status, Is.EqualTo(m_Machines[i].Status));
                    Assert.That(output[i].Deleted, Is.EqualTo(m_Machines[i].Deleted));
                    Assert.That(output[i].Disabled, Is.EqualTo(m_Machines[i].Disabled));
                }
            );
        }
    }

    [Test]
    public void MachineOutputToString()
    {
        MachinesOutput output = new(m_Machines!);
        var sb = new StringBuilder();
        foreach (var machine in output)
        {
            sb.AppendLine($"- id: {machine.Id}");
            sb.AppendLine($"  ip: {machine.Ip}");
            sb.AppendLine($"  name: {machine.Name}");
            sb.AppendLine($"  locationName: {machine.LocationName}");
            sb.AppendLine($"  locationId: {machine.LocationId}");
            sb.AppendLine($"  fleetName: {machine.FleetName}");
            sb.AppendLine($"  fleetId: {machine.FleetId}");
            sb.AppendLine($"  hardwareType: {machine.HardwareType}");
            sb.AppendLine($"  osFamily: {machine.OsFamily}");
            sb.AppendLine($"  osName: {machine.OsName}");
            sb.AppendLine($"  serversStates:");
            sb.AppendLine($"    allocated: {machine.ServersStates.Allocated}");
            sb.AppendLine($"    available: {machine.ServersStates.Available}");
            sb.AppendLine($"    held: {machine.ServersStates.Held}");
            sb.AppendLine($"    online: {machine.ServersStates.Online}");
            sb.AppendLine($"    reserved: {machine.ServersStates.Reserved}");
            sb.AppendLine($"  spec:");
            sb.AppendLine($"    cpuCores: {machine.Spec.CpuCores}");
            sb.AppendLine($"    cpuShortname: {machine.Spec.CpuShortname}");
            sb.AppendLine($"    cpuSpeed: {machine.Spec.CpuSpeed}");
            sb.AppendLine($"    cpuType: {machine.Spec.CpuType}");
            sb.AppendLine($"    memory: {machine.Spec.Memory}");
            sb.AppendLine($"  status: {machine.Status}");
            sb.AppendLine("  deleted: false");
            sb.AppendLine("  disabled: false");
        }

        Assert.That(output.ToString(), Is.EqualTo(sb.ToString()));
    }
}
