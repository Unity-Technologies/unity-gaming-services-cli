using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class MachinesItemOutput
{
    public MachinesItemOutput(Machine machine)
    {
        Id = machine.Id;
        Ip = machine.Ip;
        Name = machine.Name;
        LocationId = machine.LocationId;
        LocationName = machine.LocationName;
        FleetId = machine.FleetId;
        FleetName = machine.FleetName;
        HardwareType = machine.HardwareType;
        OsFamily = machine.OsFamily;
        OsName = machine.OsName;
        ServersStates = new MachinesItemServerStatsOutput(machine.ServersStates);
        Spec = new MachinesItemSpecOutput(machine.Spec);
        Status = machine.Status;
        Deleted = machine.Deleted;
        Disabled = machine.Disabled;
    }

    public long Id { get; }

    public string Ip { get; }

    public string Name { get; }

    public string LocationName { get; }

    public long LocationId { get; }

    public string FleetName { get; }

    public Guid FleetId { get; }

    public Machine.HardwareTypeEnum HardwareType { get; }

    public Machine.OsFamilyEnum OsFamily { get; }

    public string OsName { get; }

    public MachinesItemServerStatsOutput ServersStates { get; }

    public MachinesItemSpecOutput Spec { get; }

    public Machine.StatusEnum Status { get; }

    public bool Deleted { get; }

    public bool Disabled { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
