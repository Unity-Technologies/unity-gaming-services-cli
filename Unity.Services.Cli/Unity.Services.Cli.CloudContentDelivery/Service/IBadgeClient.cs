using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface IBadgeClient
{
    Task<ApiResponse<List<CcdGetBucket200ResponseLastReleaseBadgesInner>>> ListBadgeAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int? page,
        int? perPage,
        string filterName,
        string releaseNum,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default);

    Task<CcdGetBucket200ResponseLastReleaseBadgesInner> CreateBadgeAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string badgeName,
        long releaseNum,
        CancellationToken cancellationToken = default);

    Task<string> DeleteBadgeAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string badgeName,
        CancellationToken cancellationToken = default);

    Task AuthorizeServiceAsync(CancellationToken cancellationToken = default);
}
