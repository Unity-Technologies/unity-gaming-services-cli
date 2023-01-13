using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class BulkUpdateLobbyHandler
    {
        /// <summary>
        /// Handler for the Bulk Update Lobby command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task BulkUpdateLobbyAsync(RequiredBodyInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            var environmentId = await unityEnvironment.FetchIdentifierAsync();

            var response = await service.BulkUpdateLobbyAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                input.LobbyId!,
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.JsonFileOrBody, isRequired: true),
                cancellationToken);
            logger.LogResultValue(response);
        }
    }
}
