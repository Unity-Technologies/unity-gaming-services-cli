using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class ServersItemOutput
{
    public ServersItemOutput(Server server)
    {
        Id = server.Id;
        Ip = server.Ip;
        Port = server.Port;
        MachineId = server.MachineID;
        LocationId = server.LocationID;
        LocationName = server.LocationName;
        FleetId = server.FleetID;
        FleetName = server.FleetName;
        BuildConfigurationId = server.BuildConfigurationID;
        BuildConfigurationName = server.BuildConfigurationName;
        BuildName = server.BuildName;
        Deleted = server.Deleted;
    }

    public long Id { get; }

    public string Ip { get; }

    public int Port { get; }

    public long MachineId { get; }

    public string LocationName { get; }

    public long LocationId { get; }

    public string FleetName { get; }

    public Guid FleetId { get; }

    public string BuildConfigurationName { get; }

    public long BuildConfigurationId { get; }

    public string BuildName { get; }

    public bool Deleted { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
