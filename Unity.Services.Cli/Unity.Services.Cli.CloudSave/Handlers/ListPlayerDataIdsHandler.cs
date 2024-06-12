using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudSave.Handlers;

static class ListPlayerDataIdsHandler
{
    public static async Task ListPlayerDataIdsAsync(ListDataIdsInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Fetching resources...", _ =>
            ListPlayerDataIdsAsync(input, unityEnvironment, cloudSaveDataService, logger, cancellationToken));
    }

    internal static async Task ListPlayerDataIdsAsync(
        ListDataIdsInput input,
        IUnityEnvironment unityEnvironment,
        ICloudSaveDataService cloudSaveDataService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        var response = await cloudSaveDataService.ListPlayerDataIdsAsync(
            projectId: projectId,
            environmentId: environmentId,
            start: input.Start!,
            limit: input.Limit!,
            cancellationToken: cancellationToken);

        logger.LogResultValue(input.IsJson ? response : response.ToJson());
    }
}
