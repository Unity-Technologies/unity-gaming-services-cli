using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class FleetGetOutput
{
    public FleetGetOutput(Fleet fleet)
    {
        Name = fleet.Name;
        Id = fleet.Id;
        OsFamily = fleet.OsFamily;
        OsName = fleet.OsName;
        Status = fleet.Status;
        BuildConfigurations = fleet.BuildConfigurations;
        FleetRegions = fleet.FleetRegions;
        Servers = fleet.Servers;
        AllocationTtl = fleet.AllocationTTL;
        DeleteTtl = fleet.DeleteTTL;
        DisabledDeleteTtl = fleet.DisabledDeleteTTL;
        ShutdownTtl = fleet.ShutdownTTL;
    }

    public string Name { get; }

    public Guid Id { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Fleet.OsFamilyEnum? OsFamily { get; }

    public string OsName { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Fleet.StatusEnum Status { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public List<BuildConfiguration2> BuildConfigurations { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public List<FleetRegion1> FleetRegions { get; }

    [YamlMember(SerializeAs = typeof(Servers))]
    public Servers Servers { get; }

    public long AllocationTtl { get; }

    public long DeleteTtl { get; }

    public long DisabledDeleteTtl { get; }

    public long ShutdownTtl { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
