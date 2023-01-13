using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class GetJoinedLobbiesHandler
    {
        /// <summary>
        /// Handler for the Get Joined Lobbies command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task GetJoinedLobbiesAsync(PlayerInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync();

            var response = await service.GetJoinedLobbiesAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                input.PlayerId,
                cancellationToken);
            logger.LogResultValue(response);
        }
    }
}
