using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine1;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class MachinesOutput : List<MachinesItemOutput>
{
    public MachinesOutput(IReadOnlyCollection<Machine>? machines)
    {
        if (machines != null) AddRange(machines.Select(m => new MachinesItemOutput(m)));
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
