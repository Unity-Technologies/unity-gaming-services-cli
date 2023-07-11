using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildInstallsItemRegionsOutput : List<BuildInstallsItemRegionsItemOutput>
{
    public BuildInstallsItemRegionsOutput(IEnumerable<RegionsInner> regions)
    {
        AddRange(regions.Select(region => new BuildInstallsItemRegionsItemOutput(region)));
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
