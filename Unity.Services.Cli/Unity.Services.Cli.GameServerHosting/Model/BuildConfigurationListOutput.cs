using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildConfigurationListOutput : List<BuildConfigurationItemOutput>
{
    public BuildConfigurationListOutput(IReadOnlyCollection<BuildConfigurationListItem>? buildConfiguration)
    {
        if (buildConfiguration != null) AddRange(buildConfiguration.Select(bc => new BuildConfigurationItemOutput(bc)));
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
