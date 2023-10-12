using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class MachinesItemSpecOutput
{
    public MachinesItemSpecOutput(MachineSpec machineSpec)
    {
        CpuCores = machineSpec.CpuCores;
        CpuShortname = machineSpec.CpuShortname;
        CpuSpeed = machineSpec.CpuSpeed;
        CpuType = machineSpec.CpuType;
        Memory = machineSpec.Memory;
    }

    public long CpuCores { get; }

    public string CpuShortname { get; }

    public long CpuSpeed { get; }

    public string CpuType { get; }

    public long Memory { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
