using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class FleetRegionCreateOutput
{
    public FleetRegionCreateOutput(NewFleetRegion region)
    {
        FleetRegionId = region.Id;
        MaxServers = region.MaxServers;
        MinAvailableServers = region.MinAvailableServers;
        RegionId = region.RegionID;
        RegionName = region.RegionName;
    }

    public Guid FleetRegionId { get; }

    public long MaxServers { get; }

    public long MinAvailableServers { get; }

    public Guid RegionId { get; }

    public string RegionName { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
