using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;

static class ListEntryHandler
{
    public static async Task ListAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "List entries...",
            _ => ListAsync(
                input,
                unityEnvironment,
                entryClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task ListAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketNameOpt!;
        var currentPage = input.Page!;
        var perPage = input.PerPage!;
        var sortBy = input.SortByEntry!;
        var sortOrder = input.SortOrder!;
        var startingAfter = input.StartingAfter!;
        var path = input.Path!;
        var label = input.Label!;
        var contentType = input.ContentType!;
        bool? complete = input.Complete == true ? true : null;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await entryClient.ListEntryAsync(
            projectId,
            environmentId,
            bucketId,
            currentPage,
            startingAfter,
            perPage,
            path,
            label,
            contentType,
            complete,
            sortBy,
            sortOrder,
            cancellationToken);

        var paginationInformation = CcdUtils.GetPaginationInformation(result);
        if (paginationInformation != null) logger.LogInformation(paginationInformation);
        var entryList = result.Data
            .Select(
                e => new ListEntryResult(e.Entryid.ToString(), e.Path));

        logger.LogResultValue(entryList);

    }
}
