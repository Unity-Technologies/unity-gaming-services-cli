using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;

static class PromotionBucketHandler
{
    public static async Task PromotionStatusAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching promotion status...",
            _ => PromotionStatusAsync(
                input,
                unityEnvironment,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task PromotionStatusAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var promotionId = input.PromotionId!;
        var bucketName = input.BucketNameOpt!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        var result = await bucketClient.GetPromotionAsync(
            projectId,
            environmentId,
            bucketId,
            promotionId,
            cancellationToken);
        logger.LogResultValue(new PromotionResult(result));
    }
}
