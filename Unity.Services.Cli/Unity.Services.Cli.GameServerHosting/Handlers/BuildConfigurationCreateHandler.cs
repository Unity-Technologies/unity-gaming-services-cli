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
        var cores = input.Cores ?? 0;
        var memory = input.Memory ?? 0;
        var name = input.Name ?? throw new MissingInputException(BuildConfigurationCreateInput.NameKey);
        var queryType = input.QueryType ?? throw new MissingInputException(BuildConfigurationCreateInput.QueryTypeKey);
        var speed = input.Speed ?? 0;
        var readiness = input.Readiness ?? false;

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var parsedConfigs = configuration.Select(ParseConfig).ToList();

        var createReq = new BuildConfigurationCreateRequest(
            binaryPath: binaryPath,
            buildID: buildId,
            commandLine: commandLine,
            configuration: parsedConfigs,
            name: name,
            queryType: queryType,
            readiness: readiness
        );

        // allow for usage backwards compatibility.
        var oldUsage = cores + memory + speed;
        if (oldUsage > 0)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            createReq.Speed = speed > 0 ? speed : throw new InvalidLegacyInputUsageException(BuildConfigurationCreateInput.SpeedKey);
            createReq.Memory = memory > 0 ? memory : throw new InvalidLegacyInputUsageException(BuildConfigurationCreateInput.MemoryKey);
            createReq.Cores = cores > 0 ? cores : throw new InvalidLegacyInputUsageException(BuildConfigurationCreateInput.CoresKey);
#pragma warning disable CS0612 // Type or member is obsolete

            // log warning for those options
            logger.LogWarning("The '--cores', '--memory' and '--speed' options are deprecated and will be removed in a future release. Please use '--usage-setting' option on the fleet create instead. For more info please refer to https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/guides/configure-server-density.");
        }

        var buildConfiguration = await service.BuildConfigurationsApi.CreateBuildConfigurationAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            createReq,
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
