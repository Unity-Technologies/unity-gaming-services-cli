using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class FleetRegionUpdateOutput
{
    public FleetRegionUpdateOutput(UpdatedFleetRegion region)
    {
        DeleteTtl = region.DeleteTTL;
        DisabledDeleteTtl = region.DisabledDeleteTTL;
        Id = region.Id;
        MaxServers = region.MaxServers;
        MinAvailableServers = region.MinAvailableServers;
        RegionId = region.RegionID;
        RegionName = region.RegionName;
        ScalingEnabled = region.ScalingEnabled;
        ShutdownTtl = region.ShutdownTTL;
    }

    public long DeleteTtl { get; }

    public long DisabledDeleteTtl { get; }

    public Guid Id { get; }

    public long MaxServers { get; }

    public long MinAvailableServers { get; }

    public Guid RegionId { get; }

    public string RegionName { get; }

    public bool ScalingEnabled { get; }

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
