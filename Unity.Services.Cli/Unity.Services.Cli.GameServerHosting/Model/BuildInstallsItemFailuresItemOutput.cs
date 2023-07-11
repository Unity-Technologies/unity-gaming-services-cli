using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildInstallsItemFailuresItemOutput
{
    public BuildInstallsItemFailuresItemOutput(BuildListInner1FailuresInner failure)
    {
        MachineId = failure.MachineID;
        Reason = failure.Reason;
        Updated = failure.Updated;
    }

    public long MachineId { get; }

    public string Reason { get; }

    [YamlMember(SerializeAs = typeof(string))]
    public DateTime Updated { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
