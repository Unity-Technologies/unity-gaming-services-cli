using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;

static class GetEntryHandler
{
    public static async Task GetAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Getting entry...",
            _ => GetAsync(
                input,
                unityEnvironment,
                entryClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task GetAsync(
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

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await entryClient.GetEntryAsync(
            projectId,
            environmentId,
            bucketId,
            entryPath,
            versionId,
            cancellationToken);
        logger.LogResultValue(new EntryResult(result));
    }
}
