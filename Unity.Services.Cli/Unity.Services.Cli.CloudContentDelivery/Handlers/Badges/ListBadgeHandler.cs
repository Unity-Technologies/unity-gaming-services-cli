using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Badges;

static class ListBadgeHandler
{
    public static async Task ListAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBadgeClient badgeClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching badges list...",
            _ => ListAsync(
                input,
                unityEnvironment,
                badgeClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task ListAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IBadgeClient badgeClient,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var currentPage = input.Page!;
        var perPage = input.PerPage!;
        var bucketName = input.BucketNameOpt!;
        var releaseNum = input.ReleaseNumOption!;
        var filterName = input.FilterName!;
        var sortBy = input.SortByBadge!;
        var sortOrder = input.SortOrder!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await badgeClient.ListBadgeAsync(
            projectId,
            environmentId,
            bucketId,
            currentPage,
            perPage,
            filterName,
            releaseNum,
            sortBy,
            sortOrder,
            cancellationToken);

        var paginationInformation = CcdUtils.GetPaginationInformation(result);
        if (paginationInformation != null) logger.LogInformation(paginationInformation);
        var badgeList = result.Data
            .Select(
                e => new ListBadgeResult(e.Releaseid.ToString(), e.Releasenum, e.Name));
        logger.LogResultValue(badgeList);

    }
}
