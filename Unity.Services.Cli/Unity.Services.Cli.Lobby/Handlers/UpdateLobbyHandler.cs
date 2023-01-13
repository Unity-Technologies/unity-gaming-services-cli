using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class UpdateLobbyHandler
    {
        /// <summary>
        /// Handler for the Update Lobby command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task UpdateLobbyAsync(RequiredBodyInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync();

            var response = await service.UpdateLobbyAsync(
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
