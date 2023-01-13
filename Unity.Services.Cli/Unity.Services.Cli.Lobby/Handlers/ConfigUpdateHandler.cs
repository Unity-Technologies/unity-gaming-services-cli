using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.RemoteConfig.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class ConfigUpdateHandler
    {
        /// <param name="input">
        /// Lobby input automatically parsed. So developer does not need to retrieve from ParseResult.
        /// </param>
        /// <param name="service">
        /// The instance of <see cref="IRemoteConfigService"/> used to make API requests.
        /// </param>
        /// <param name="logger">
        /// A singleton logger to log output for commands.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that should be propagated as much as possible to allow the command operations to be cancelled at any time.
        /// </param>
        public static async Task ConfigUpdateAsync(LobbyConfigUpdateInput input, IRemoteConfigService service, ILogger logger, CancellationToken cancellationToken)
        {
            var projectId = input.CloudProjectId ?? throw new MissingConfigurationException(
                Keys.ConfigKeys.ProjectId, Keys.EnvironmentKeys.ProjectId);

            await service.UpdateConfigAsync(
                projectId,
                input.ConfigId!,
                RequestBodyHandler.GetRequestBodyFromFileOrInput(input.JsonFileOrBody!, isRequired: true),
                cancellationToken);
            logger.LogInformation("Config successfully updated.");
        }
    }
}
