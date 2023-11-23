using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class FleetListItemOutput
{
    public FleetListItemOutput(FleetListItem fleet)
    {
        Name = fleet.Name;
        Id = fleet.Id;
        OsName = fleet.OsName;
        Status = fleet.Status;
        BuildConfigurations = fleet.BuildConfigurations;
        Regions = fleet.Regions;
        Servers = fleet.Servers;
        UsageSettings = fleet.UsageSettings;
    }

    public string Name { get; }

    public Guid Id { get; }

    public string OsName { get; }

    public FleetListItem.StatusEnum Status { get; }

    public List<BuildConfiguration1> BuildConfigurations { get; }

    public List<FleetRegion> Regions { get; }

    public Servers Servers { get; }

    public List<FleetUsageSetting> UsageSettings { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
