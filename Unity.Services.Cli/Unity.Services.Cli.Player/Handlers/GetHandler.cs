using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Service;

namespace Unity.Services.Cli.Player.Handlers;

public static class GetHandler
{
    public static async Task GetAsync(PlayerInput input, IPlayerService playerService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Getting Player Information ...",  _ =>
            GetAsync(input, playerService, logger, cancellationToken));
    }

    internal static async Task GetAsync(PlayerInput input, IPlayerService playerService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var playerId = input.PlayerId!;
        var playerResponse = await playerService.GetAsync(projectId, playerId, cancellationToken);

        logger.LogResultValue(playerResponse);
    }
}
