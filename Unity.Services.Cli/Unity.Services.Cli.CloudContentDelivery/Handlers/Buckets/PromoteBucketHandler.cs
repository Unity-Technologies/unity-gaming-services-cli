using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;

static class PromoteBucketHandler
{
    public static async Task PromoteAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IReleaseClient releaseClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Promoting bucket...",
            _ => PromoteAsync(
                input,
                unityEnvironment,
                releaseClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task PromoteAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IReleaseClient releaseClient,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketNameOpt!;
        var fromReleaseNum = (int)input.ReleaseNum!;
        var notes = input.Notes!;
        var targetBucketName = input.TargetBucketName!;
        var toEnvironment = await unityEnvironment.FetchIdentifierFromSpecificEnvironmentNameAsync(input.TargetEnvironment!, cancellationToken);

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        var toBucket = await bucketClient.GetBucketIdByName(
            projectId,
            toEnvironment,
            targetBucketName,
            cancellationToken);

        var fromReleaseId = await releaseClient.GetReleaseIdByNumber(
            projectId,
            environmentId,
            bucketId,
            fromReleaseNum,
            cancellationToken);

        var result = await bucketClient.PromoteBucketEnvAsync(
            projectId,
            environmentId,
            bucketId,
            fromReleaseId,
            notes,
            toBucket,
            toEnvironment,
            cancellationToken);
        logger.LogResultValue(new PromoteResult(result));
    }
}
