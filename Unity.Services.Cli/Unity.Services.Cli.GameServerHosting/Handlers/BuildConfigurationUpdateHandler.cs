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

static class BuildConfigurationUpdateHandler
{
    public static async Task BuildConfigurationUpdateAsync(
        BuildConfigurationUpdateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Updating build config...",
            _ =>
                BuildConfigurationUpdateAsync(
                    input,
                    unityEnvironment,
                    service,
                    logger,
                    cancellationToken));
    }

    internal static async Task BuildConfigurationUpdateAsync(
        BuildConfigurationUpdateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var currentConfig = await service.BuildConfigurationsApi.GetBuildConfigurationAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            input.BuildConfigId,
            cancellationToken: cancellationToken
        );

        List<ConfigurationPair1> configurationParam;

        if (input.Configuration != null && input.Configuration?.Count != 0)
        {
            configurationParam = input.Configuration!.Select(ParseConfig).ToList();
        }
        else
        {
            configurationParam = currentConfig._Configuration.Select(ConvertEntryToPair).ToList();
        }

        // API requires all these fields to be populated, to make it a nicer user experience we populate
        // Null input values with the existing values
        var req = new BuildConfigurationUpdateRequest(
#pragma warning disable CS0612 // Type or member is obsolete
            binaryPath: input.BinaryPath ?? currentConfig.BinaryPath,
            buildID: (input.BuildId is not null && input.BuildId != 0) ? input.BuildId.Value : currentConfig.BuildID,
            commandLine: input.CommandLine ?? currentConfig.CommandLine,
            configuration: configurationParam,
            cores: input.Cores ?? currentConfig.Cores,
            memory: input.Memory ?? currentConfig.Memory,
            name: input.Name ?? currentConfig.Name,
            queryType: input.QueryType ?? currentConfig.QueryType,
            speed: input.Speed ?? currentConfig.Speed
#pragma warning restore CS0612 // Type or member is obsolete
        );

        var buildConfiguration = await service.BuildConfigurationsApi.UpdateBuildConfigurationAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            input.BuildConfigId,
            req,
            cancellationToken: cancellationToken);

        logger.LogResultValue(new BuildConfigurationOutput(buildConfiguration));
    }

    static ConfigurationPair1 ParseConfig(string rawKv)
    {
        var parts = rawKv.Split(':');
        if (parts.Length != 2)
        {
            throw new InvalidKeyValuePairException(rawKv);
        }

        return new ConfigurationPair1(key: parts[0], value: parts[1]);
    }

    static ConfigurationPair1 ConvertEntryToPair(ConfigEntry entry)
    {
        return new ConfigurationPair1(id: entry.Id, key: entry.Key, value: entry.Value);
    }
}
