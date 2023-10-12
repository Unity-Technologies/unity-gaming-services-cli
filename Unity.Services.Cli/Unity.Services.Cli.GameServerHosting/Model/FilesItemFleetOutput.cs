using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class FilesItemFleetOutput
{
    public FilesItemFleetOutput(FleetDetails fleetDetails)
    {
        Id = fleetDetails.Id;
        Name = fleetDetails.Name;
    }

    public Guid Id { get; }
    public string Name { get; }


    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
