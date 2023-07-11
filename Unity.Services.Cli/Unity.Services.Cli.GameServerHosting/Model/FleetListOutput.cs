using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class FleetListOutput : List<FleetListItemOutput>
{
    public FleetListOutput(IReadOnlyCollection<FleetListItem>? fleets)
    {
        if (fleets != null) AddRange(fleets.Select(f => new FleetListItemOutput(f)));
    }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
