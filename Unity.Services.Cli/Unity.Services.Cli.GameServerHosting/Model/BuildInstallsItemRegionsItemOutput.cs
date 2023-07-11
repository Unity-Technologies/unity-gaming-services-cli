using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildInstallsItemRegionsItemOutput
{
    public BuildInstallsItemRegionsItemOutput(RegionsInner region)
    {
        RegionName = region.RegionName;
        PendingMachines = region.PendingMachines;
        CompletedMachines = region.CompletedMachines;
        Failures = region.Failures;
    }

    public string RegionName { get; }
    public long PendingMachines { get; }
    public long CompletedMachines { get; }
    public long Failures { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
