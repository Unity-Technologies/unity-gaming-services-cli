using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class QuickJoinHandler
    {
        /// <summary>
        /// Handler for the QuickJoin command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task QuickJoinAsync(CommonLobbyInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

            var response = await service.QuickJoinAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.QueryFilter, isRequired: true),
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.PlayerDetails, isRequired: true),
                cancellationToken);
            logger.LogResultValue(response);
        }
    }
}
