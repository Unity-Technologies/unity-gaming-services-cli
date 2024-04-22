using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface ISynchronizationService
{
    Task<SyncResult> CalculateSynchronization(
        string projectId,
        string environmentId,
        string bucketId,
        string localFolder,
        string? exclusionPattern,
        bool delete,
        List<string>? labels,
        object? metadata,
        CancellationToken cancellationToken);

    SyncResult CalculateDifference(
        string projectId,
        string environmentId,
        string bucketId,
        string localFolder,
        bool deleteIfFileNotPresentInLocalFolder,
        List<CcdCreateOrUpdateEntryBatch200ResponseInner> remoteEntries,
        HashSet<string> localFiles,
        List<string>? labels,
        object? metadata);

    Task<List<CcdCreateReleaseRequestEntriesInner>> ProcessSynchronization(
        ILogger logger,
        bool verbose,
        SyncResult syncResult,
        string localFolder,
        int retryCount,
        int maxConcurrentRequests,
        int retryDelayMilliseconds,
        CancellationToken cancellationToken);

    HashSet<string> GetFilesFromDir(
        string directoryPath,
        string? exclusionPattern);

    Task AuthorizeServiceAsync(CancellationToken cancellationToken = default);
}
