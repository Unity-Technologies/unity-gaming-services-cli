using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class DeleteLobbyHandler
    {
        /// <summary>
        /// Handler for the Delete Lobby command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task DeleteLobbyAsync(CommonLobbyInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

            await service.DeleteLobbyAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                input.PlayerId,
                input.LobbyId!,
                cancellationToken);
            logger.LogInformation("Lobby successfully deleted.");
        }
    }
}
