using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class CoreDumpOutput
{
    public CoreDumpOutput(GetCoreDumpConfig200Response coreDump)
    {
        StorageType = coreDump.StorageType?.ToString().ToLower() ?? "unknown";
        Credentials = new CoreDumpCredentialsOutput(coreDump.Credentials);
        FleetId = coreDump.FleetId;
        State = CoreDumpStateConverter.ConvertToString(coreDump.State);
        UpdatedAt = coreDump.UpdatedAt;
    }

    public Guid FleetId { get; set; }
    public string StorageType { get; set; }
    public string State { get; set; }
    public CoreDumpCredentialsOutput Credentials { get; set; }
    public DateTime UpdatedAt { get; set; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Serialize(this);
    }
}
