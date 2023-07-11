using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class UpdatePlayerHandler
    {
        /// <summary>
        /// Handler for the Update Player command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task UpdatePlayerAsync(PlayerInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

            var response = await service.UpdatePlayerAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                input.PlayerId,
                input.LobbyId!,
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.JsonFileOrBody, isRequired: true),
                cancellationToken);
            logger.LogResultValue(response);
        }
    }
}
