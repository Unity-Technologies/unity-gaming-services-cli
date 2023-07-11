using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class BuildConfigurationCreateHandler
{
    public static async Task BuildConfigurationCreateAsync(
        BuildConfigurationCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Creating build config...", _ =>
            BuildConfigurationCreateAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task BuildConfigurationCreateAsync(
        BuildConfigurationCreateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        var binaryPath = input.BinaryPath ?? throw new MissingInputException(BuildConfigurationCreateInput.BinaryPathKey);
        var buildId = input.BuildId ?? throw new MissingInputException(BuildConfigurationCreateInput.BuildIdKey);
        var commandLine = input.CommandLine ?? throw new MissingInputException(BuildConfigurationCreateInput.CommandLineKey);
        var configuration = input.Configuration ?? throw new MissingInputException(BuildConfigurationCreateInput.ConfigurationKey);
        var cores = input.Cores ?? throw new MissingInputException(BuildConfigurationCreateInput.CoresKey);
        var memory = input.Memory ?? throw new MissingInputException(BuildConfigurationCreateInput.MemoryKey);
        var name = input.Name ?? throw new MissingInputException(BuildConfigurationCreateInput.NameKey);
        var queryType = input.QueryType ?? throw new MissingInputException(BuildConfigurationCreateInput.QueryTypeKey);
        var speed = input.Speed ?? throw new MissingInputException(BuildConfigurationCreateInput.SpeedKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var parsedConfigs = configuration.Select(ParseConfig).ToList();

        var buildConfiguration = await service.BuildConfigurationsApi.CreateBuildConfigurationAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            new BuildConfigurationCreateRequest(
                binaryPath: binaryPath,
                buildID: buildId,
                commandLine: commandLine,
                configuration: parsedConfigs,
                cores: cores,
                memory: memory,
                name: name,
                queryType: queryType,
                speed: speed
            ),
            cancellationToken: cancellationToken);
        logger.LogResultValue(new BuildConfigurationOutput(buildConfiguration));
    }

    static ConfigurationPair ParseConfig(string rawKv)
    {
        var parts = rawKv.Split(':');
        if (parts.Length != 2)
        {
            throw new InvalidKeyValuePairException(rawKv);
        }

        return new ConfigurationPair(key: parts[0], value: parts[1]);
    }
}
