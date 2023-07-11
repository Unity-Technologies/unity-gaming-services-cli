using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildInstallsItemOutput
{
    public BuildInstallsItemOutput(BuildListInner1 install)
    {
        FleetName = install.FleetName;
        // Ccd can be null, the codegen doesn't handle this case
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (install.Ccd != null) Ccd = new CcdOutput(install.Ccd);
        Container = install.Container;
        PendingMachines = install.PendingMachines;
        CompletedMachines = install.CompletedMachines;
        Failures = new BuildInstallsItemFailuresOutput(install.Failures);
        Regions = new BuildInstallsItemRegionsOutput(install.Regions);
    }

    public string FleetName { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public CcdOutput? Ccd { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public ContainerImage Container { get; }

    public long PendingMachines { get; }

    public long CompletedMachines { get; }

    public BuildInstallsItemFailuresOutput Failures { get; }

    public BuildInstallsItemRegionsOutput Regions { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
