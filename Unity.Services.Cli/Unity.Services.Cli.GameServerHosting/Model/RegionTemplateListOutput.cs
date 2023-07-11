using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class RegionTemplateListOutput : List<RegionTemplateListItemOutput>
{
    public RegionTemplateListOutput(IReadOnlyCollection<FleetRegionsTemplateListItem>? regions)
    {
        if (regions != null) AddRange(regions.Select(f => new RegionTemplateListItemOutput(f)));
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
