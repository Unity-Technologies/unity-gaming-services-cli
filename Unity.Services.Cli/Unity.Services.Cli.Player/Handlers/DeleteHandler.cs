using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Service;

namespace Unity.Services.Cli.Player.Handlers;

public static class DeleteHandler
{
    public static async Task DeleteAsync(PlayerInput input, IPlayerService playerService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Deleting Player...",  _ =>
            DeleteAsync(input, playerService, logger, cancellationToken));
    }

    internal static async Task DeleteAsync(PlayerInput input, IPlayerService playerService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var playerId = input.PlayerId!;
        await playerService.DeleteAsync(projectId, playerId, cancellationToken);

        logger.LogInformation("Player '{playerId}' deleted.",playerId);
    }
}
