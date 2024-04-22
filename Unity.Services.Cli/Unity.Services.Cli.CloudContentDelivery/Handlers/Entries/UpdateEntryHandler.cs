using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;

static class UpdateEntryHandler
{
    public static async Task UpdateAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Updating Entry...",
            _ => UpdateAsync(
                input,
                unityEnvironment,
                entryClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task UpdateAsync(
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
        var versionId = input.VersionId!;
        var labels = input.Labels!;
        var metadata = input.Metadata!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await entryClient.UpdateEntryAsync(
            projectId,
            environmentId,
            bucketId,
            entryPath,
            versionId,
            labels,
            metadata,
            cancellationToken);
        logger.LogResultValue(new EntryResult(result));
    }
}
