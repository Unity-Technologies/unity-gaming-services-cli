using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Service;

namespace Unity.Services.Cli.Player.Handlers;

public static class DisableHandler
{
    public static async Task DisableAsync(PlayerInput input, IPlayerService playerService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Disabling Player...",  _ =>
            DisableAsync(input, playerService, logger, cancellationToken));
    }

    internal static async Task DisableAsync(PlayerInput input, IPlayerService playerService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var playerId = input.PlayerId!;
        await playerService.DisableAsync(projectId, playerId, cancellationToken);

        logger.LogInformation("Player '{playerId}' disabled.",  playerId);
    }
}
