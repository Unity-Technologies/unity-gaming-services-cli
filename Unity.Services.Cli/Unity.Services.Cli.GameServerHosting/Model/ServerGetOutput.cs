using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class ServerGetOutput
{
    public ServerGetOutput(Server server)
    {
        BuildConfigurationId = server.BuildConfigurationID;
        BuildConfigurationName = server.BuildConfigurationName;
        BuildName = server.BuildName;
        Deleted = server.Deleted;
        FleetId = server.FleetID;
        FleetName = server.FleetName;
        HardwareType = server.HardwareType;
        Id = server.Id;
        Ip = server.Ip;
        LocationId = server.LocationID;
        LocationName = server.LocationName;
        MachineId = server.MachineID;
        Port = server.Port;
        Status = server.Status;
    }

    public long BuildConfigurationId { get; }

    public string BuildConfigurationName { get; }

    public string BuildName { get; }

    public bool Deleted { get; }

    public Guid FleetId { get; }

    public string FleetName { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Server.HardwareTypeEnum HardwareType { get; }

    public long Id { get; }

    public string Ip { get; }

    public long LocationId { get; }

    public string LocationName { get; }

    public long MachineId { get; }

    public int Port { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Server.StatusEnum Status { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
