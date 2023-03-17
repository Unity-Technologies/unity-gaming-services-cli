using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Service;

namespace Unity.Services.Cli.Player.Handlers;

public static class EnableHandler
{
    public static async Task EnableAsync(PlayerInput input, IPlayerService playerService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Enabling Player...",  _ =>
            EnableAsync(input, playerService, logger, cancellationToken));
    }

    internal static async Task EnableAsync(PlayerInput input, IPlayerService playerService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var playerId = input.PlayerId!;
        await playerService.EnableAsync(projectId, playerId, cancellationToken);

        logger.LogInformation("Player '{playerId}' enabled.",  playerId);
    }
}
