using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Handlers;

static class FileListHandler
{
    public static async Task FileListAsync(
        FileListInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken
    )
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching files list...",
            _ => FileListAsync(
                input,
                unityEnvironment,
                service,
                logger,
                cancellationToken));
    }

    internal static async Task FileListAsync(
        FileListInput input,
        IUnityEnvironment unityEnvironment,
        IGameServerHostingService service,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        // FetchIdentifierAsync handles null checks for project-id and environment
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var serverIdValues = input.ServerIds ?? throw new MissingInputException(FileListInput.ServerIdKey);

        await service.AuthorizeGameServerHostingService(cancellationToken);

        var request = new FilesListRequest(
            limit: long.Parse(input.Limit!),
            pathFilter: input.PathFilter!,
            serverIds: serverIdValues.ToList()
        );

        if (input.ModifiedFrom != null)
        {
            request.ModifiedFrom = DateTime.Parse(input.ModifiedFrom).ToUniversalTime();
        }

        if (input.ModifiedTo != null)
        {
            request.ModifiedTo = DateTime.Parse(input.ModifiedTo).ToUniversalTime();
        }

        var files = await service.FilesApi.ListFilesAsync(
            Guid.Parse(input.CloudProjectId!),
            Guid.Parse(environmentId),
            request,
            cancellationToken: cancellationToken
        );

        logger.LogResultValue(new FilesOutput(files));
    }
}
