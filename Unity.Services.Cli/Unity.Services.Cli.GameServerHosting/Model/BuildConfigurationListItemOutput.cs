using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildConfigurationItemOutput
{
    public BuildConfigurationItemOutput(BuildConfigurationListItem buildConfiguration)
    {
        Name = buildConfiguration.Name;
        Id = buildConfiguration.Id;
        BuildName = buildConfiguration.BuildName;
        BuildId = buildConfiguration.BuildID;
        FleetName = buildConfiguration.FleetName;
        FleetId = buildConfiguration.FleetID;
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

    public long Version { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull, SerializeAs = typeof(string))]
    public DateTime? CreatedAt { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull, SerializeAs = typeof(string))]
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
