using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class BuildConfigurationListHandler
{
    public static async Task BuildConfigurationListAsync(
        BuildConfigurationListInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching build configuration list...", _ =>
            BuildConfigurationListAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task BuildConfigurationListAsync(
        BuildConfigurationListInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        Guid? fleetId = null;
        string? partial = null;

        if (input.FleetId != null)
        {
            fleetId = Guid.Parse(input.FleetId);
        }

        if (input.Partial != null)
        {
            partial = input.Partial;
        }

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var buildconfigs = await service.BuildConfigurationsApi.ListBuildConfigurationsAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            fleetId,
            partial,
            cancellationToken: cancellationToken);

        logger.LogResultValue(new BuildConfigurationListOutput(buildconfigs));
    }
}
