using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Buckets;

static class ListBucketHandler
{
    public static async Task ListAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching buckets list...",
            _ => ListAsync(
                input,
                unityEnvironment,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task ListAsync(
        CloudContentDeliveryInputBuckets input,
        IUnityEnvironment unityEnvironment,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var currentPage = input.Page!;
        var perPage = input.PerPage!;
        var filterName = input.FilterName!;
        var sortBy = input.SortByBucket!;
        var sortOrder = input.SortOrder!;
        var description = input.BucketDescription!;
        var result = await bucketClient.ListBucketAsync(
            projectId,
            environmentId,
            currentPage,
            perPage,
            filterName,
            description,
            sortBy,
            sortOrder,
            cancellationToken);

        var paginationInformation = CcdUtils.GetPaginationInformation(result);
        if (paginationInformation != null) logger.LogInformation(paginationInformation);

        var bucketsList = result.Data
            .Select(
                e => new ListBucketResult(e.Id.ToString(), e.Name));
        logger.LogResultValue(bucketsList);
    }
}
