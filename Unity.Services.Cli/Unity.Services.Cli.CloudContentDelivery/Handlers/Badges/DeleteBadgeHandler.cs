using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Badges;

static class DeleteBadgeHandler
{
    public static async Task DeleteAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBadgeClient badgeClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Deleting badge...",
            _ => DeleteAsync(
                input,
                unityEnvironment,
                badgeClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task DeleteAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBadgeClient badgeClient,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var badgeName = input.BadgeName!;
        var bucketName = input.BucketNameOpt!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        await badgeClient.DeleteBadgeAsync(
            projectId,
            environmentId,
            bucketId,
            badgeName,
            cancellationToken);
        logger.LogInformation("Badge {badgeName} deleted.", badgeName);
    }
}
