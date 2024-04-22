using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;

static class PermissionBucketHandler
{
    public static async Task PermissionUpdateAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Updating permissions...",
            _ => PermissionUpdateAsync(
                input,
                unityEnvironment,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task PermissionUpdateAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketName!;

        var result = await bucketClient.UpdatePermissionBucketAsync(
            projectId,
            environmentId,
            bucketName,
            input.Action,
            input.Permission,
            input.Role,
            cancellationToken);
        logger.LogResultValue(new PermissionResult(result));

    }
}
