using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Machine = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.Machine;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class FilesItemMachineOutput
{
    public FilesItemMachineOutput(Machine machine)
    {
        Id = machine.Id;
        Location = machine.Location;
    }

    public long Id { get; }
    public string Location { get; }


    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
