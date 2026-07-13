using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using EnvironmentConfig = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model.EnvironmentConfig;

namespace Unity.Services.Cli.Matchmaker.Model;

class EnvironmentConfigOutput
{
    public bool? Enabled { get; set; }
    public string? DefaultQueueName { get; set; }

    public EnvironmentConfigOutput(EnvironmentConfig config)
    {
        Enabled = config.Enabled;
        DefaultQueueName = config.DefaultQueueName;
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
