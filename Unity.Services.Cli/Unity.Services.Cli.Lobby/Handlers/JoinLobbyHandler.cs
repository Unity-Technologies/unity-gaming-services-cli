using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class JoinLobbyHandler
    {
        /// <summary>
        /// Handler for the Join Lobby command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task JoinLobbyAsync(JoinInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync();

            var response = await service.JoinLobbyAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                input.LobbyId,
                input.LobbyCode,
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.PlayerDetails, isRequired: true),
                cancellationToken);
            logger.LogResultValue(response);
        }
    }
}
