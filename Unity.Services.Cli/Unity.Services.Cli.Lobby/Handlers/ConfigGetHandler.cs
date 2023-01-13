using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.RemoteConfig.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    static class ConfigGetHandler
    {
        private const string k_LobbyConfigType = "lobby";

        private const string k_EnvironmentNotFoundMsg = $"The value of option '{Keys.ConfigKeys.EnvironmentName}' fed as " +
                                                        "direct input, found in configuration or fetched from " +
                                                        "environment variables does not correspond to an environment " +
                                                        "that exists. Command will proceed without a specific " +
                                                        $"{Keys.ConfigKeys.EnvironmentName}.";

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
        public static async Task ConfigGetAsync(CommonInput input, IUnityEnvironment unityEnvironment,
            IRemoteConfigService service, ILogger logger, CancellationToken cancellationToken)
        {
            var projectId = input.CloudProjectId ?? throw new MissingConfigurationException(
                Keys.ConfigKeys.ProjectId, Keys.EnvironmentKeys.ProjectId);
            string? environmentId = null;

            try
            {
                environmentId = await unityEnvironment.FetchIdentifierAsync();
            }
            catch (MissingConfigurationException)
            {
                /*
                 * This exception is thrown when 'environment' is missing, but since 'environment' is an option and
                 * not an argument here, we ignore this exception.
                 */
            }
            catch (EnvironmentNotFoundException)
            {
                logger.LogWarning(k_EnvironmentNotFoundMsg);
            }

            var result = await service.GetAllConfigsFromEnvironmentAsync(
                projectId,
                environmentId,
                k_LobbyConfigType,
                cancellationToken);
            logger.LogResultValue(result);
        }
    }
}
