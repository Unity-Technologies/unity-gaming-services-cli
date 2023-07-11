using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class BuildConfigurationDeleteHandler
{
    public static async Task BuildConfigurationDeleteAsync(
        BuildConfigurationIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Deleting build configuration...", _ =>
            BuildConfigurationDeleteAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task BuildConfigurationDeleteAsync(
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

        await service.BuildConfigurationsApi.DeleteBuildConfigurationAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            buildConfigurationId,
            cancellationToken: cancellationToken);

        logger.LogInformation("Build configuration deleted successfully");
    }
}
