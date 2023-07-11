using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildConfigurationOutput
{
    public BuildConfigurationOutput(BuildConfiguration buildConfiguration)
    {
        Id = buildConfiguration.Id;
        Name = buildConfiguration.Name;
        BuildName = buildConfiguration.BuildName;
        BuildId = buildConfiguration.BuildID;
        FleetName = buildConfiguration.FleetName;
        FleetId = buildConfiguration.FleetID;
        BinaryPath = buildConfiguration.BinaryPath;
        CommandLine = buildConfiguration.CommandLine;
        QueryType = buildConfiguration.QueryType;
        Configuration = buildConfiguration._Configuration;
        Cores = buildConfiguration.Cores;
        Speed = buildConfiguration.Speed;
        Memory = buildConfiguration.Memory;
        Version = buildConfiguration._Version;
        CreatedAt = buildConfiguration.CreatedAt;
        UpdatedAt = buildConfiguration.UpdatedAt;
    }

    public string Name { get; }
    public long Id { get; }
    public string BuildName { get; }
    public long BuildId { get; }
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public string FleetName { get; }
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public Guid FleetId { get; }
    public string BinaryPath { get; }
    public string CommandLine { get; }
    public string QueryType { get; }
    public List<ConfigEntry> Configuration { get; }
    public long Cores { get; }
    public long Speed { get; }
    public long Memory { get; }
    public long Version { get; }
    public DateTime? CreatedAt { get; }
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public DateTime? UpdatedAt { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
