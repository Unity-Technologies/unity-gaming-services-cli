using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class QueryLobbiesHandler
    {
        /// <summary>
        /// Handler for the Query Lobbies command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task QueryLobbiesAsync(CommonLobbyInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

            var response = await service.QueryLobbiesAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                input.PlayerId,
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.JsonFileOrBody),
                cancellationToken);
            logger.LogResultValue(response);
        }
    }
}
