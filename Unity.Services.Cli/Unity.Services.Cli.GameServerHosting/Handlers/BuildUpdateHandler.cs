using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class BuildUpdateHandler
{
    public static async Task BuildUpdateAsync(
        BuildUpdateInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync("Updating build...", _ =>
            BuildUpdateAsync(input, unityEnvironment, service, logger, cancellationToken));
    }

    internal static async Task BuildUpdateAsync(
        BuildUpdateInput input,
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
        var newBuildName = input.BuildName ?? throw new MissingInputException(BuildUpdateInput.NameKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);
        await service.BuildsApi.UpdateBuildAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            buildId,
            new UpdateBuildRequest(newBuildName),
            cancellationToken: cancellationToken);

        logger.LogInformation("Build updated successfully");
    }
}
