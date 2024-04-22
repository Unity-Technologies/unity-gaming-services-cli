using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;

static class DeleteEntryHandler
{
    public static async Task DeleteAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting entry...",
            _ => DeleteAsync(
                input,
                unityEnvironment,
                entryClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task DeleteAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketNameOpt!;
        var entryPath = input.EntryPath!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await entryClient.DeleteEntryAsync(
            projectId,
            environmentId,
            bucketId,
            entryPath,
            cancellationToken);
        logger.LogInformation(result);
    }
}
