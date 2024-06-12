using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudSave.Models;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudSave.Handlers;

static class ListIndexesHandler
{
    public static async Task ListIndexesAsync(CommonInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService, ILogger logger,
        ILoadingIndicator loadingIndicator, CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Fetching resources...", _ =>
            ListIndexesAsync(input, unityEnvironment, cloudSaveDataService, logger, cancellationToken));
    }

    internal static async Task ListIndexesAsync(CommonInput input, IUnityEnvironment unityEnvironment, ICloudSaveDataService cloudSaveDataService,
        ILogger logger, CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId!;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);

        var response = await cloudSaveDataService.ListIndexesAsync(
            projectId: projectId,
            environmentId: environmentId,
            cancellationToken: cancellationToken);
        var result = new ListIndexesOutput(response);

        logger.LogResultValue(result);
    }
}
