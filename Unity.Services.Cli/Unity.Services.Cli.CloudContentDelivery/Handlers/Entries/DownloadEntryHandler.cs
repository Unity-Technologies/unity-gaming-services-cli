using Microsoft.Extensions.Logging;
using Unity.Services.Cli.CloudContentDelivery.Input;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudContentDelivery.Handlers.Entries;

static class DownloadEntryHandler
{
    const int k_BufferSize = 8192;

    public static async Task DownloadAsync(
        CloudContentDeliveryInput input,
        IUnityEnvironment unityEnvironment,
        IEntryClient entryClient,
        IBucketClient bucketClient,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Downloading entry content...",
            _ => DownloadAsync(
                input,
                unityEnvironment,
                entryClient,
                bucketClient,
                logger,
                cancellationToken));
    }

    internal static async Task DownloadAsync(
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
        var path = input.EntryPath!;
        var versionId = input.VersionId!;

        var bucketId = await bucketClient.GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        CcdUtils.ValidateBucketIdIsPresent(bucketId);

        var result = await entryClient.DownloadEntryAsync(
            projectId,
            environmentId,
            bucketId,
            path,
            versionId,
            cancellationToken);

        FileStream fileStream;
        await using (fileStream = new FileStream(
                         Path.GetFileName(path),
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         k_BufferSize,
                         true))
        {
            await result.CopyToAsync(fileStream, cancellationToken);
            fileStream.Close();
        }

    }
}
