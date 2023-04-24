using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Model;
using Unity.Services.Cli.Player.Service;

namespace Unity.Services.Cli.Player.Handlers;

public static class ListHandler
{
    public static async Task ListAsync(PlayerInput input, IPlayerService playerService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Listing Players Information ...",  _ =>
            ListAsync(input, playerService, logger, cancellationToken));
    }

    internal static async Task ListAsync(PlayerInput input, IPlayerService playerService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var limit = input.Limit;
        var page = input.PlayersPage;
        var playerResponse = await playerService.ListAsync(projectId, limit, page ,cancellationToken);

        var result = new PlayerListResponseResult(playerResponse);

        logger.LogResultValue(result);
    }
}
