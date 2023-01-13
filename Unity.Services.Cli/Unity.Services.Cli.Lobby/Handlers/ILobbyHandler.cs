using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;

namespace Unity.Services.Cli.Lobby.Handlers
{
    // Interface to centralize the common documentation for Lobby command handlers.
    interface ILobbyHandler
    {
        /// <param name="input">
        /// Lobby input automatically parsed. So developer does not need to retrieve from ParseResult.
        /// </param>
        /// <param name="service">
        /// The instance of <see cref="ILobbyService"/> used to make API requests.
        /// </param>
        /// <param name="logger">
        /// A singleton logger to log output for commands.
        /// </param>
        /// <param name="context">
        /// An invoke context to pass to ExceptionHelper to set exit code in case of command failure.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that should be propagated as much as possible to allow the command operations to be cancelled at any time.
        /// </param>
        public Task Handler(CommonLobbyInput input, ILobbyService service, ILogger logger, CancellationToken cancellationToken);
    }
}
