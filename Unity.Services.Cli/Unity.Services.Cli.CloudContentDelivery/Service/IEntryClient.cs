using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface IEntryClient
{
    Task<CcdCreateOrUpdateEntryBatch200ResponseInner?> UpdateEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string entryPath,
        string versionId,
        List<string>? labels,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task<CcdCreateOrUpdateEntryBatch200ResponseInner?> GetEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string entryPath,
        string versionId,
        CancellationToken cancellationToken = default);

    public Task<Stream> DownloadEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string path,
        string versionId,
        CancellationToken cancellationToken = default);

    Task<string> DeleteEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string entryPath,
        CancellationToken cancellationToken = default);

    Task<CcdCreateOrUpdateEntryBatch200ResponseInner?> CopyEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string localPath,
        string remotePath,
        List<string> labels,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<CcdCreateOrUpdateEntryBatch200ResponseInner>>> ListEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int? page,
        string? startingAfter,
        int? perPage,
        string? path,
        string? label,
        string? contentType,
        bool? complete,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default);

    Task AuthorizeServiceAsync(CancellationToken cancellationToken = default);
}
