using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class RegionTemplateListItemOutput
{
    public RegionTemplateListItemOutput(FleetRegionsTemplateListItem region)
    {
        Name = region.Name;
        RegionId = region.RegionID;
    }

    public string Name { get; }

    public Guid RegionId { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
