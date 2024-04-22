using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;

static class CopyEntryHandler
{
    public static async Task CopyAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Copying entry...",
            _ => CopyAsync(
                input,
                unityEnvironment,
                entryClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task CopyAsync(
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
        var localPath = input.LocalPath!;
        var remotePath = input.RemotePath!;
        var labels = input.Labels!;
        var metadata = input.Metadata!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);
        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await entryClient.CopyEntryAsync(
            projectId,
            environmentId,
            bucketId,
            localPath,
            remotePath,
            labels,
            metadata,
            cancellationToken);

        logger.LogResultValue(new EntryResult(result));

    }
}
