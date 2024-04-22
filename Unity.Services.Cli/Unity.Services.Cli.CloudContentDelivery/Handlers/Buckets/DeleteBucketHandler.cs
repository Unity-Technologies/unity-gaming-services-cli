using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;

static class DeleteBucketHandler
{
    public static async Task DeleteAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting bucket...",
            _ => DeleteAsync(
                input,
                unityEnvironment,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task DeleteAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketName!;
        var result = await bucketClient.DeleteBucketAsync(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);
        logger.LogResultValue(result);
    }
}
