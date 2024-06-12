using Microsoft.Extensions.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Services;

class GameServerHostingConfigLoader : IGameServerHostingConfigLoader
{
    internal const string k_Extension = ".gsh";

    readonly IDeployFileService m_DeployFileService;
    readonly IMultiplayConfigValidator m_ConfigValidator;
    readonly ILogger m_Logger;

    public GameServerHostingConfigLoader(IDeployFileService deployFileService, IMultiplayConfigValidator configValidator, ILogger logger)
    {
        m_DeployFileService = deployFileService;
        m_ConfigValidator = configValidator;
        m_Logger = logger;
    }

    public async Task<MultiplayConfig> LoadAndValidateAsync(ICollection<string> paths, CancellationToken cancellationToken)
    {
        var configLoadTasks = m_DeployFileService
            .ListFilesToDeploy(paths, k_Extension)
            .Select(async path => (path, config: await LoadConfig(m_DeployFileService, path, cancellationToken)))
            .ToList();
        var configs = await Task.WhenAll(configLoadTasks);

        ValidateConfigs(m_Logger, m_ConfigValidator, configs);
        var merged = MergeConfigs(configs.Select(c => c.config));

        return merged;
    }

    static void ValidateConfigs(ILogger logger, IMultiplayConfigValidator configValidator, IEnumerable<(string path, MultiplayConfig config)> configs)
    {
        var errors = new List<(string path, IMultiplayConfigValidator.Error error)>();
        foreach (var (path, config) in configs)
        {
            errors.AddRange(configValidator.Validate(config).Select(error => (path, error)));
        }

        if (errors.Any())
        {
            foreach (var (path, error) in errors)
            {
                logger.LogError("{}: {}", path, error.Message);
            }

            throw new InvalidConfigException(errors.First().path);
        }
    }

    static async Task<MultiplayConfig> LoadConfig(IDeployFileService fileService, string path, CancellationToken cancellationToken)
    {
        var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new ResourceNameTypeConverter())
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

        return deserializer.Deserialize<MultiplayConfig>(await fileService.ReadAllTextAsync(path, cancellationToken));
    }

    static MultiplayConfig MergeConfigs(IEnumerable<MultiplayConfig> configs)
    {
        var merged = new MultiplayConfig
        {
            Version = "1.0"
        };
        foreach (var config in configs)
        {
            foreach (var build in config.Builds ?? new Dictionary<BuildName, MultiplayConfig.BuildDefinition>())
                merged.Builds.Add(build);

            foreach (var buildConfig in config.BuildConfigurations ?? new Dictionary<BuildConfigurationName, MultiplayConfig.BuildConfigurationDefinition>())
                merged.BuildConfigurations.Add(buildConfig);

            foreach (var fleet in config.Fleets ?? new Dictionary<FleetName, MultiplayConfig.FleetDefinition>())
                merged.Fleets.Add(fleet);
        }
        return merged;
    }

}
