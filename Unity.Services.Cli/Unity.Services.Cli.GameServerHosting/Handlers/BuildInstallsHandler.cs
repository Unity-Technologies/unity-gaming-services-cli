using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class BuildInstallsHandler
{
    public static async Task BuildInstallsAsync(
        BuildIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Fetching installs...", _ =>
            BuildInstallsAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task BuildInstallsAsync(
        BuildIdInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var buildIdStr = input.BuildId ?? throw new MissingInputException(BuildIdInput.BuildIdKey);
        var buildId = long.Parse(buildIdStr);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var build = await service.BuildsApi.GetBuildInstallsAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            buildId,
            cancellationToken: cancellationToken);

        logger.LogResultValue(new BuildInstallsOutput(build));
    }
}
