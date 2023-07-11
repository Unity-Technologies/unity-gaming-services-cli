using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class BuildConfigurationGetHandler
{
    public static async Task BuildConfigurationGetAsync(
        BuildConfigurationIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching build configuration...", _ =>
            BuildConfigurationGetAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task BuildConfigurationGetAsync(
        BuildConfigurationIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var buildConfigurationIdStr = input.BuildConfigurationId ?? throw new MissingInputException(BuildConfigurationIdInput.BuildConfigurationIdKey);
        var buildConfigurationId = long.Parse(buildConfigurationIdStr);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var buildConfiguration = await service.BuildConfigurationsApi.GetBuildConfigurationAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            buildConfigurationId,
            cancellationToken: cancellationToken);

        logger.LogResultValue(new BuildConfigurationOutput(buildConfiguration));
    }
}
