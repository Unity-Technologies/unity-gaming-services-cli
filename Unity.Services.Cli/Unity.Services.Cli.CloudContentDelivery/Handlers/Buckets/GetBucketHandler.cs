using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;

static class GetBucketHandler
{
    public static async Task GetAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching bucket info...",
            _ => GetAsync(
                input,
                unityEnvironment,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task GetAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketName!;

        var result = await bucketClient.GetBucketAsync(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);
        logger.LogResultValue(new BucketResult(result));
    }
}
