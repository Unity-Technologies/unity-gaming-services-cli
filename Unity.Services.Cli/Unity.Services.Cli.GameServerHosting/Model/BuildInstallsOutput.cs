using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildInstallsOutput : List<BuildInstallsItemOutput>
{
    public BuildInstallsOutput(IReadOnlyCollection<BuildListInner1>? installs)
    {
        if (installs != null) AddRange(installs.Select(b => new BuildInstallsItemOutput(b)));
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
