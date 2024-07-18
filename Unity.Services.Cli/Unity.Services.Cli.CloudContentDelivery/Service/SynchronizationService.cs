using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public class SynchronizationService : ISynchronizationService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IContentDeliveryValidator m_ContentDeliveryValidator;
    readonly IEntriesApi m_EntriesApi;
    readonly IFileSystem m_FileSystem;
    readonly IUploadContentClient m_UploadContentClient;
    const double k_BytesInMb = 1048576.0;
    const int k_MaximumPageSizeAllowed = 100;

    public SynchronizationService(
        IEntriesApi entriesApi,
        IUploadContentClient uploadContentClient,
        IFileSystem fileSystem,
        IServiceAccountAuthenticationService authenticationService,
        IContentDeliveryValidator contentDeliveryValidator)
    {
        m_EntriesApi = entriesApi;
        m_UploadContentClient = uploadContentClient;
        m_FileSystem = fileSystem;
        m_AuthenticationService = authenticationService;
        m_ContentDeliveryValidator = contentDeliveryValidator;
    }

    internal static async Task RespectRateLimitAsync(
        Multimap<string, string> headers,
        SharedRateLimitStatus rateLimitStatus,
        CancellationToken cancellationToken)
    {
        if (!headers.TryGetValue("unity-ratelimit", out var rateLimitValues))
            return;

        var rateLimit = rateLimitValues.FirstOrDefault();
        if (string.IsNullOrEmpty(rateLimit))
            return;

        var limits = rateLimit.Split(';');
        foreach (var limit in limits)
        {
            var parts = limit.Split(',');
            var remainingPart = parts.FirstOrDefault(p => p.Contains("remaining"));
            var resetPart = parts.FirstOrDefault(p => p.Contains("reset"));

            if (remainingPart == null || resetPart == null)
                continue;

            var remaining = int.Parse(remainingPart.Split('=')[1]);
            var resetInSeconds = int.Parse(resetPart.Split('=')[1]);
            if (remaining == 0)
            {
                var resetTime = TimeSpan.FromSeconds(resetInSeconds);
                rateLimitStatus.UpdateRateLimit(true, resetTime);
                await Task.Delay(resetTime, cancellationToken);
                rateLimitStatus.UpdateRateLimit(false, TimeSpan.Zero);
                return;
            }
        }
    }

    public async Task<List<CcdCreateOrUpdateEntryBatch200ResponseInner>> FetchAllEntriesAsync(
        string projectId,
        string environmentId,
        string bucketId,
        CancellationToken cancellationToken)
    {
        var rateLimitStatus = new SharedRateLimitStatus();
        var allEntries = new List<CcdCreateOrUpdateEntryBatch200ResponseInner>();
        Guid? lastEntryId = null;
        var hasMorePages = true;
        while (hasMorePages)
        {
            var pageResponse = await FetchPageAsync(
                projectId,
                environmentId,
                bucketId,
                lastEntryId,
                lastEntryId == null ? 10 : k_MaximumPageSizeAllowed,
                rateLimitStatus,
                cancellationToken);

            if (pageResponse.Data.Count == 0)
            {
                hasMorePages = false;
            }
            else
            {
                allEntries.AddRange(pageResponse.Data.Where(entry => entry.Complete));
                lastEntryId = pageResponse.Data.Last().Entryid;
            }
        }

        return allEntries;
    }

    async Task<ApiResponse<List<CcdCreateOrUpdateEntryBatch200ResponseInner>>> FetchPageAsync(
        string projectId,
        string environmentId,
        string bucketId,
        Guid? lastEntryId,
        int perPage,
        SharedRateLimitStatus rateLimitStatus,
        CancellationToken cancellationToken)
    {
        if (rateLimitStatus.IsRateLimited) await Task.Delay(rateLimitStatus.ResetTime, cancellationToken);

        var response = await m_EntriesApi.GetEntriesEnvWithHttpInfoAsync(
            environmentId,
            bucketId,
            projectId,
            null,
            lastEntryId,
            perPage,
            null,
            null,
            null,
            null,
            null,
            null,
            0,
            cancellationToken);

        await RespectRateLimitAsync(response.Headers, rateLimitStatus, cancellationToken);
        return response;

    }

    public async Task<SyncResult> CalculateSynchronization(
        string projectId,
        string environmentId,
        string bucketId,
        string localFolder,
        string? exclusionPattern,
        bool delete,
        List<string>? labels,
        object? metadata,
        CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        m_ContentDeliveryValidator.ValidateBucketId(bucketId);

        // Get Remote files
        var remoteEntries = await FetchAllEntriesAsync(
            projectId,
            environmentId,
            bucketId,
            cancellationToken);

        // Get Local files
        var localFiles = GetFilesFromDir(localFolder, exclusionPattern);

        // Synchronize files (Diff between Local and Remote)
        return CalculateDifference(
            projectId,
            environmentId,
            bucketId,
            localFolder,
            delete,
            remoteEntries,
            localFiles,
            labels,
            metadata);

    }

    public SyncResult CalculateDifference(
        string projectId,
        string environmentId,
        string bucketId,
        string localFolder,
        bool deleteIfFileNotPresentInLocalFolder,
        List<CcdCreateOrUpdateEntryBatch200ResponseInner> remoteEntries,
        HashSet<string> localFiles,
        List<string>? labels,
        object? metadata)
    {

        var syncResult = new SyncResult();

        foreach (var remoteEntry in remoteEntries)
        {
            var path = CcdUtils.ConvertPathToForwardSlashes(remoteEntry.Path);

            if (localFiles.Contains(path))
            {
                var filePath = Path.Combine(localFolder, path);
                filePath = CcdUtils.AdjustPathForPlatform(filePath);
                try
                {
                    using (var filestream = m_FileSystem.File.OpenRead(filePath))
                    {
                        var contentType = m_UploadContentClient.GetContentType(filePath);
                        var contentSize = m_UploadContentClient.GetContentSize(filestream);
                        var contentHash = m_UploadContentClient.GetContentHash(filestream);

                        var currentEntry = new SyncEntry(
                            path,
                            environmentId,
                            bucketId,
                            projectId,
                            contentSize,
                            contentType,
                            contentHash,
                            remoteEntry.Entryid.ToString(),
                            remoteEntry.CurrentVersionid.ToString(),
                            labels,
                            metadata);

                        if (contentSize != remoteEntry.ContentSize ||
                            !string.Equals(remoteEntry.ContentType, contentType, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(remoteEntry.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
                            syncResult.EntriesToUpdate.Add(currentEntry);
                        else
                            syncResult.EntriesToSkip.Add(currentEntry);
                    }

                    // Remove from local files
                    localFiles.Remove(path);
                }
                catch (Exception ex)
                {
                    throw new CliException($"Error processing entry '{filePath}': {ex.Message}", ExitCode.HandledError);
                }
            }
            else if (deleteIfFileNotPresentInLocalFolder)
            {
                // Remote files not present in local folder are marked to be deleted.
                syncResult.EntriesToDelete.Add(
                    new SyncEntry(
                        path,
                        environmentId,
                        bucketId,
                        projectId,
                        0L,
                        "",
                        "",
                        remoteEntry.Entryid.ToString(),
                        remoteEntry.CurrentVersionid.ToString()));
            }
            else
            {
                // Remote files not present in local folder are to be skipped.
                var entryToSkip = new SyncEntry(
                    path)
                {
                    EntryId = remoteEntry.Entryid.ToString(),
                    VersionId = remoteEntry.CurrentVersionid.ToString()
                };
                syncResult.EntriesToSkip.Add(
                    entryToSkip
                );
            }
        }

        // Add remaining local files to EntriesToAdd.
        foreach (var path in localFiles)
        {
            var filePath = Path.Combine(localFolder, path);
            filePath = CcdUtils.AdjustPathForPlatform(filePath);
            using var filestream = m_FileSystem.File.OpenRead(filePath);
            var contentSize = m_UploadContentClient.GetContentSize(filestream);
            var contentType = m_UploadContentClient.GetContentType(filePath);
            var contentHash = m_UploadContentClient.GetContentHash(filestream);

            syncResult.EntriesToAdd.Add(
                new SyncEntry(
                    path,
                    environmentId,
                    bucketId,
                    projectId,
                    contentSize,
                    contentType,
                    contentHash,
                    null,
                    null,
                    labels,
                    metadata
                ));
        }

        return syncResult;
    }

    public async Task<List<CcdCreateReleaseRequestEntriesInner>> ProcessSynchronization(
        ILogger logger,
        bool verbose,
        SyncResult syncResult,
        string localFolder,
        int retryCount,
        int maxConcurrentRequests,
        int retryDelayMilliseconds,
        CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        var rateLimitStatus = new SharedRateLimitStatus();
        var semaphore = new SemaphoreSlim(maxConcurrentRequests);


        var addBatches = CreateBatches(syncResult.EntriesToAdd, 20);
        var updateBatches = CreateBatches(syncResult.EntriesToUpdate, 20);
        var deleteBatches = CreateBatches(syncResult.EntriesToDelete, 20);

        var tasks = addBatches.Select(
                batch => ProcessBatchAddOrUpdateEntryAsync(
                    logger,
                    verbose,
                    batch,
                    localFolder,
                    retryCount,
                    retryDelayMilliseconds,
                    semaphore,
                    rateLimitStatus,
                    cancellationToken))
            .ToList();

        tasks.AddRange(
            updateBatches.Select(
                    batch => ProcessBatchAddOrUpdateEntryAsync(
                        logger,
                        verbose,
                        batch,
                        localFolder,
                        retryCount,
                        retryDelayMilliseconds,
                        semaphore,
                        rateLimitStatus,
                        cancellationToken))
                .ToList());

        tasks.AddRange(
            deleteBatches.Select(
                    batch => ProcessBatchDeleteEntryAsync(
                        logger,
                        verbose,
                        batch,
                        retryCount,
                        retryDelayMilliseconds,
                        semaphore,
                        rateLimitStatus,
                        cancellationToken))
                .ToList());

        try
        {
            var results = await Task.WhenAll(tasks);
            CollectSyncEntriesVersion(syncResult, results, out var releaseEntries);
            return releaseEntries;
        }
        catch (Exception ex)
        {
            throw new CliException(
                $"At least one of the task failed during the synchronization: {ex.Message}",
                ExitCode.HandledError);
        }

    }

    static List<List<SyncEntry>> CreateBatches(List<SyncEntry> entries, int batchSize)
    {
        var batches = new List<List<SyncEntry>>();
        for (int i = 0; i < entries.Count; i += batchSize)
        {
            batches.Add(entries.GetRange(i, Math.Min(batchSize, entries.Count - i)));
        }
        return batches;
    }


    static void CollectSyncEntriesVersion(
        SyncResult syncResult,
        List<CcdCreateReleaseRequestEntriesInner>[] results,
        out List<CcdCreateReleaseRequestEntriesInner> releaseEntries)
    {

        releaseEntries = new List<CcdCreateReleaseRequestEntriesInner>();
        foreach (var taskResult in results) releaseEntries.AddRange(taskResult);
        releaseEntries.AddRange(
            syncResult.EntriesToSkip
                .Select(
                    entry => new CcdCreateReleaseRequestEntriesInner(
                        new Guid(entry.EntryId),
                        new Guid(entry.VersionId))));
    }

    async Task<List<CcdCreateReleaseRequestEntriesInner>> ProcessBatchAddOrUpdateEntryAsync(
       ILogger logger,
       bool verbose,
       List<SyncEntry> entryBatch,
       string localFolder,
       int retryCount,
       int retryDelayMilliseconds,
       SemaphoreSlim semaphore,
       SharedRateLimitStatus rateLimitStatus,
       CancellationToken cancellationToken)
    {

        var releaseEntries = new List<CcdCreateReleaseRequestEntriesInner>();
        await ThrottledRetryPolicyAsync(
            logger,
            verbose,
            retryCount,
            retryDelayMilliseconds,
            semaphore,
            async () =>
            {

                if (entryBatch.Count == 0)
                    return;

                var ccdCreateOrUpdateEntryBatchRequestInner = entryBatch.Select(
                        entry => new CcdCreateOrUpdateEntryBatchRequestInner(
                            entry.ContentHash,
                            entry.ContentSize,
                            entry.ContentType,
                            entry.Labels ?? new List<string>(),
                            entry.Metadata ?? "",
                            entry.Path))
                    .ToList();

                if (rateLimitStatus.IsRateLimited) await Task.Delay(rateLimitStatus.ResetTime, cancellationToken);

                var createdEntriesResponse = await m_EntriesApi.CreateOrUpdateEntryBatchEnvWithHttpInfoAsync(
                    entryBatch.First().EnvironmentId,
                    entryBatch.First().BucketId,
                    entryBatch.First().ProjectId,
                    ccdCreateOrUpdateEntryBatchRequestInner,
                    0,
                    cancellationToken);


                await RespectRateLimitAsync(createdEntriesResponse.Headers, rateLimitStatus, cancellationToken);

                var uploadSemaphore = new SemaphoreSlim(10);
                var tasks = new List<Task>();

                foreach (var createdEntry in createdEntriesResponse.Data)
                {
                    await uploadSemaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var filePath = Path.Combine(localFolder, createdEntry.Path);
                            filePath = CcdUtils.AdjustPathForPlatform(filePath);
                            await using var filestream = m_FileSystem.File.OpenRead(filePath);
                            var response = await m_UploadContentClient.UploadContentToCcd(
                                createdEntry.SignedUrl,
                                filestream,
                                cancellationToken);
                            if (response.IsSuccessStatusCode)
                                releaseEntries.Add(
                                    new CcdCreateReleaseRequestEntriesInner(
                                        createdEntry.Entryid,
                                        createdEntry.CurrentVersionid));
                            else
                                throw new Exception($"Error syncing entry {createdEntry.Path}: {response.ReasonPhrase}.");
                        }
                        finally
                        {
                            uploadSemaphore.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);

                // Add small throttling between batches
                await Task.Delay(500, cancellationToken);

            });
        return releaseEntries;
    }

    async Task<List<CcdCreateReleaseRequestEntriesInner>> ProcessBatchDeleteEntryAsync(
       ILogger logger,
       bool verbose,
       List<SyncEntry> entryBatch,
       int retryCount,
       int retryDelayMilliseconds,
       SemaphoreSlim semaphore,
       SharedRateLimitStatus rateLimitStatus,
       CancellationToken cancellationToken)
    {
        await ThrottledRetryPolicyAsync(
            logger,
            verbose,
            retryCount,

            retryDelayMilliseconds,
            semaphore,
            async () =>
            {
                if (entryBatch.Count == 0)
                    return;

                var listEntries = entryBatch.Select(
                        entry => new CcdDeleteEntryBatchRequestInner(
                            new Guid(entry.EntryId)))
                    .ToList();

                if (rateLimitStatus.IsRateLimited) await Task.Delay(rateLimitStatus.ResetTime, cancellationToken);

                var response = await m_EntriesApi.DeleteEntryBatchEnvWithHttpInfoAsync(
                    entryBatch.First().EnvironmentId,
                    entryBatch.First().BucketId,
                    entryBatch.First().ProjectId,
                    listEntries,
                    0,
                    cancellationToken);
                await RespectRateLimitAsync(response.Headers, rateLimitStatus, cancellationToken);

            });
        return new List<CcdCreateReleaseRequestEntriesInner>();
    }

    static async Task ThrottledRetryPolicyAsync(
        ILogger logger,
        bool verbose,
        int retryCount,
        int retryDelayMilliseconds,
        SemaphoreSlim semaphore,
        Func<Task> action)
    {

        var currentRetry = 0;
        while (currentRetry <= retryCount)
            try
            {
                await semaphore.WaitAsync();
                await action();

                if (verbose && currentRetry >= 1)
                    logger.LogWarning(
                        $"Synchronization Operation {Math.Abs(action.GetHashCode())} succeeded after retry {currentRetry + 1}/{retryCount}.");
                break;
            }
            catch (Exception ex)
            {
                currentRetry++;
                if (currentRetry > retryCount)
                    throw new CliException(
                        $"Error, the synchronization failed: {ex.Message}",
                        ExitCode.HandledError);
                if (verbose)
                    logger.LogWarning(
                    $"Synchronization operation {Math.Abs(action.GetHashCode())} failed (attempt {currentRetry}/{retryCount}): {ex.Message}");

                await Task.Delay(retryDelayMilliseconds);
            }
            finally
            {
                semaphore.Release();
            }
    }

    public static double CalculateUploadSpeed(double totalDataInMegabytes, double timeInSeconds)
    {
        var totalDataInMegabits = totalDataInMegabytes * 8;
        var uploadSpeedMbps = totalDataInMegabits / timeInSeconds;
        return uploadSpeedMbps;
    }

    public static IOperationSummary CalculateOperationSummary(
        bool verbose,
        SyncResult syncResult,
        double syncProcessTime,
        CcdGetBucket200ResponseLastRelease? release,
        CcdGetBucket200ResponseLastReleaseBadgesInner? badge)
    {
        if (verbose)
        {
            long totalUploadedSizeInBytes = 0;
            var totalFilesUploaded = 0;
            foreach (var entry in syncResult.EntriesToAdd.Concat(syncResult.EntriesToUpdate))
            {
                totalUploadedSizeInBytes += entry.ContentSize;
                totalFilesUploaded++;
            }

            var totalDataInMegabytes = totalUploadedSizeInBytes / k_BytesInMb;
            var uploadSpeed = CalculateUploadSpeed(totalDataInMegabytes, syncProcessTime);

            return new LongOperationSummary(
                syncResult,
                true,
                syncProcessTime,
                totalDataInMegabytes,
                uploadSpeed,
                totalFilesUploaded,
                release,
                badge
            );
        }
        else
        {
            return new ShortOperationSummary(
                syncResult,
                true,
                syncProcessTime,
                release,
                badge
            );
        }
    }


    public HashSet<string> GetFilesFromDir(
        string directoryPath,
        string? exclusionPattern)
    {
        if (!m_FileSystem.Directory.Exists(directoryPath))
            throw new CliException($"Directory '{directoryPath}' does not exist.", ExitCode.HandledError);

        var files = m_FileSystem.Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories);
        if (!string.IsNullOrEmpty(exclusionPattern))
        {
            var excludePatternRegex = new Regex(
                exclusionPattern,
                RegexOptions.CultureInvariant,
                TimeSpan.FromSeconds(2));
            files = files.Where(filePath => !excludePatternRegex.IsMatch(filePath));
        }

        return new HashSet<string>(
            files.Select(filePath => CcdUtils.ConvertPathToForwardSlashes(m_FileSystem.Path.GetRelativePath(directoryPath, filePath))));
    }

    public async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_EntriesApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }
}
