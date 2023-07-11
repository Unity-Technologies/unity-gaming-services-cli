using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class ServerGetHandler
{
    public static async Task ServerGetAsync(
        ServerIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching server...",
            _ => ServerGetAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task ServerGetAsync(
        ServerIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var serverId = input.ServerId ?? throw new MissingInputException(ServerIdInput.ServerIdKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var server = await service.ServersApi.GetServerAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            long.Parse(serverId),
            cancellationToken: cancellationToken);

        logger.LogResultValue(new ServerGetOutput(server));
    }
}
