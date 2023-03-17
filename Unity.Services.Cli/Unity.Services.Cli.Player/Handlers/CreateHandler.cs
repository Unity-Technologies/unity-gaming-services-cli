using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Service;

namespace Unity.Services.Cli.Player.Handlers;

public static class CreateHandler
{
    public static async Task CreateAsync(PlayerInput input, IPlayerService playerService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Creating Player...",  _ =>
            CreateAsync(input, playerService, logger, cancellationToken));
    }

    internal static async Task CreateAsync(PlayerInput input, IPlayerService playerService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var response = await playerService.CreateAsync(projectId, cancellationToken);

        logger.LogInformation("Player '{response}' created.",response.User.Id);
    }
}
