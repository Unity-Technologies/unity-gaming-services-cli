using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface IReleaseClient
{
    Task<CcdGetBucket200ResponseLastRelease> CreateReleaseAsync(
        string projectId,
        string environmentId,
        string bucketId,
        CcdCreateReleaseRequest ccdRelease,
        CancellationToken cancellationToken = default);

    Task<CcdGetBucket200ResponseLastRelease> UpdateReleaseAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int releaseNum,
        string notes,
        CancellationToken cancellationToken = default);

    Task<CcdGetBucket200ResponseLastRelease> GetReleaseAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int releaseNum,
        CancellationToken cancellationToken = default);

    Task<CcdGetBucket200ResponseLastRelease> GetReleaseByBadgeNameAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string badgeName,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<CcdGetBucket200ResponseLastRelease>>> ListReleaseAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int? page,
        int? perPage,
        string? releaseNum,
        string? promotedFromBucket,
        string? promotedFromRelease,
        string? badges,
        string? notes,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default);

    public Task<string> GetReleaseIdByNumber(
        string projectId,
        string environmentId,
        string bucketId,
        int releaseNum,
        CancellationToken cancellationToken);

    Task AuthorizeServiceAsync(CancellationToken cancellationToken = default);
}
