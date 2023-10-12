using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class MachinesItemServerStatsOutput
{
    public MachinesItemServerStatsOutput(ServersStates serversStates)
    {
        Allocated = serversStates.Allocated;
        Available = serversStates.Available;
        Held = serversStates.Held;
        Online = serversStates.Online;
        Reserved = serversStates.Reserved;
    }

    public long Allocated { get; }

    public long Available { get; }

    public long Held { get; }

    public long Online { get; }

    public long Reserved { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
