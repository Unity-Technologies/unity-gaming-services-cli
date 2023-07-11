using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class RegionAvailableHandler
{
    public static async Task RegionAvailableAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching available regions...", _ =>
            RegionAvailableAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task RegionAvailableAsync(
        FleetIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var fleetId = input.FleetId ?? throw new MissingInputException(FleetIdInput.FleetIdKey);

        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var builds = await service.FleetsApi.GetAvailableFleetRegionsAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            Guid.Parse(fleetId),
            cancellationToken: cancellationToken);

        logger.LogResultValue(new RegionTemplateListOutput(builds));
    }
}
