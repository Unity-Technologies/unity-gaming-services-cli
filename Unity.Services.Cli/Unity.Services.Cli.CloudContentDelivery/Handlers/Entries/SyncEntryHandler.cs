using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;

static class SyncEntryHandler
{

    const int k_DefaultConcurrentUploadRequests = 10;
    const int k_MaxConcurrentUploadRequests = 30;
    const int k_MinConcurrentUploadRequests = 1;

    public static async Task SyncEntriesAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IClientWrapper clients,
        ISynchronizationService synchronizationService,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Syncing entries...",
            async _ => await SyncEntriesAsync(
                input,
                unityEnvironment,
                clients,
                synchronizationService,
                logger,
                cancellationToken));
    }

    internal static async Task SyncEntriesAsync(CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IClientWrapper clients,
        ISynchronizationService synchronizationService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const int maxConcurrentBatchAddOrUpdate = 5;
        const int retryDelayMilliseconds = 5000;
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        var bucketName = input.BucketNameOpt!;
        var localFolder = input.LocalFolder!;
        var exclusionPattern = input.ExclusionPattern!;
        var delete = input.Delete == true;
        var retryCount = input.Retry ?? 3;
        var dryRun = (bool)input.DryRun!;
        var badgeName = input.UpdateBadge;
        var createRelease = input.CreateRelease == true;
        var includeSyncEntriesOnly = input.IncludeSyncEntriesOnly ?? true;
        var labels = input.Labels;
        var releaseNotes = input.ReleaseNotes!;
        var verbose = input.Verbose == true;
        var concurrentUploadRequests = input.ConcurrentUploadRequests ?? k_DefaultConcurrentUploadRequests;

        concurrentUploadRequests = ValidateConcurrentUploadRequests(logger, concurrentUploadRequests);

        HandleBadgeAndReleaseNoteWarning(logger, createRelease, badgeName,
            releaseNotes);

        var bucketId = await clients.BucketClient!.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var stopwatch = Stopwatch.StartNew();
        var newCancellationToken = cancellationToken;
        var metadata = input.SyncMetadata != null ? CcdUtils.ParseMetadata(input.SyncMetadata) : null;

        if (input.Timeout != 0)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(input.Timeout));
            newCancellationToken = cancellationTokenSource.Token;
        }

        var syncResult = await synchronizationService.CalculateSynchronization(
            projectId,
            environmentId,
            bucketId,
            localFolder,
            exclusionPattern,
            delete,
            labels,
            metadata,
            newCancellationToken);
        stopwatch.Stop();
        stopwatch.Restart();

        if (!dryRun)
        {
            var releaseEntries = await synchronizationService.ProcessSynchronization(
                logger,
                verbose,
                syncResult,
                localFolder,
                retryCount,
                maxConcurrentBatchAddOrUpdate,
                concurrentUploadRequests,
                retryDelayMilliseconds,
                cancellationToken);
            stopwatch.Stop();
            var syncProcessTime = stopwatch.Elapsed.TotalSeconds;
            CcdGetBucket200ResponseLastRelease? release = null;
            CcdGetBucket200ResponseLastReleaseBadgesInner? badge = null;
            if (createRelease)
            {

                var releaseRequest = new CcdCreateReleaseRequest()
                {
                    Notes = releaseNotes
                };

                if (includeSyncEntriesOnly)
                    releaseRequest.Entries = releaseEntries;

                if (metadata != null)
                    releaseRequest.Metadata = metadata;

                release = await clients.ReleaseClient!.CreateReleaseAsync(
                    projectId,
                    environmentId,
                    bucketId,
                    releaseRequest,
                    newCancellationToken);

                if (badgeName != null)
                    badge = await clients.BadgeClient!.CreateBadgeAsync(
                        projectId,
                        environmentId,
                        bucketId,
                        badgeName,
                        release.Releasenum,
                        newCancellationToken);

            }

            logger.LogResultValue(
                SynchronizationService.CalculateOperationSummary(
                    verbose,
                    syncResult,
                    syncProcessTime,
                    release,
                    badge));
        }
        else
        {
            logger.LogResultValue(syncResult);
        }

    }
    internal static int ValidateConcurrentUploadRequests(ILogger logger, int concurrentUploadRequests)
    {
        switch (concurrentUploadRequests)
        {
            case > k_MaxConcurrentUploadRequests:
                logger.LogWarning(
                    $"The maximum number of concurrent upload requests allowed is {k_MaxConcurrentUploadRequests}. " +
                    $"Your requested value ({concurrentUploadRequests}) was set to {k_MaxConcurrentUploadRequests}.");
                break;
            case < k_MinConcurrentUploadRequests:
                logger.LogWarning(
                    $"The minimum number of concurrent upload requests allowed is {k_MinConcurrentUploadRequests}. " +
                    $"Your requested value ({concurrentUploadRequests}) was set to {k_MinConcurrentUploadRequests}.");
                break;
        }

        return Math.Clamp(concurrentUploadRequests,
            k_MinConcurrentUploadRequests,
            k_MaxConcurrentUploadRequests);
    }


    static void HandleBadgeAndReleaseNoteWarning(ILogger logger, bool createRelease, string? badgeName, string releaseNotes)
    {
        if (createRelease) return;
        if (badgeName != null)
            logger.LogWarning(
                "The badge option requires the 'create release' option to be set to true. As a result, no badge was created or updated.");

        if (!string.IsNullOrEmpty(releaseNotes))
            logger.LogWarning(
                "The release notes option requires the 'create release' option to be set to true. As a result, no release notes were added.");
    }
}
