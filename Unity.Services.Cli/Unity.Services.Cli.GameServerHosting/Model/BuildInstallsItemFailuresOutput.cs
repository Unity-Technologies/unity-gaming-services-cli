using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildInstallsItemFailuresOutput : List<BuildInstallsItemFailuresItemOutput>
{
    public BuildInstallsItemFailuresOutput(IEnumerable<BuildListInner1FailuresInner> failures)
    {
        foreach (var failure in failures) Add(new BuildInstallsItemFailuresItemOutput(failure));
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
