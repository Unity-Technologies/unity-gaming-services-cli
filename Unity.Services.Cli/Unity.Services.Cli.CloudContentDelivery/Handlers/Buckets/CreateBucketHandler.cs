using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;

static class CreateBucketHandler
{
    public static async Task CreateAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating bucket...",
            _ => CreateAsync(
                input,
                unityEnvironment,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task CreateAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var name = input.BucketName!;
        var description = input.BucketDescription ?? "";
        var privateBucket = input.BucketPrivate ?? false;

        var result = await bucketClient.CreateBucketAsync(
            projectId,
            environmentId,
            name,
            description,
            privateBucket,
            cancellationToken);

        logger.LogResultValue(new BucketResult(result));
    }
}
