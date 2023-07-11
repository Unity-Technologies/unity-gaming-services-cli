using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class FleetGetHandler
{
    public static async Task FleetGetAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching fleet...",
            _ => FleetGetAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task FleetGetAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetIdInput.FleetIdKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var fleet = await service.FleetsApi.GetFleetAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            Guid.Parse(fleetId),
            cancellationToken: cancellationToken);

        logger.LogResultValue(new FleetGetOutput(fleet));
    }
}
