using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class CreateLobbyHandler
    {
        /// <summary>
        /// Handler for the Create Lobby command.
        /// </summary>
        /// <inheritdoc cref="ILobbyHandler.Handler" />
        public static async Task CreateLobbyAsync(RequiredBodyInput input, IUnityEnvironment unityEnvironment, ILobbyService service, ILogger logger, CancellationToken cancellationToken)
        {
            string? environmentId = await unityEnvironment.FetchIdentifierAsync();

            var response = await service.CreateLobbyAsync(
                input.CloudProjectId,
                environmentId,
                input.ServiceId,
                input.PlayerId,
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.JsonFileOrBody, isRequired: true),
                cancellationToken);
            logger.LogResultValue(response);
        }
    }
}
