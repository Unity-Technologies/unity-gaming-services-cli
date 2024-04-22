using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Badges;

static class CreateBadgeHandler
{
    public static async Task CreateAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBadgeClient badgeClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Creating badge...",
            _ => CreateAsync(
                input,
                unityEnvironment,
                badgeClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task CreateAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBadgeClient badgeClient,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketNameOpt!;
        var badgeName = input.BadgeName!;
        var releaseNum = (int)input.ReleaseNum!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await badgeClient.CreateBadgeAsync(
            projectId,
            environmentId,
            bucketId,
            badgeName,
            releaseNum,
            cancellationToken);

        logger.LogResultValue(new BadgeResult(result));
    }

}
