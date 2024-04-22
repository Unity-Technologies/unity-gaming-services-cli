using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public interface IBucketClient
{
    Task<ApiResponse<List<CcdGetBucket200Response>>> ListBucketAsync(
        string projectId,
        string environmentId,
        int? page,
        int? perPage,
        string filterName,
        string description,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default);

    Task<CcdGetBucket200Response?> GetBucketAsync(
        string projectId,
        string environmentId,
        string bucketId,
        CancellationToken cancellationToken = default);

    Task<CcdGetBucket200Response?> CreateBucketAsync(
        string projectId,
        string environmentId,
        string name,
        string description,
        bool isPrivate,
        CancellationToken cancellationToken = default);

    Task<object> DeleteBucketAsync(
        string projectId,
        string environmentId,
        string bucketId,
        CancellationToken cancellationToken = default);

    Task<CcdGetAllByBucket200ResponseInner> UpdatePermissionBucketAsync(
        string projectId,
        string environmentId,
        string bucketId,
        CcdUpdatePermissionByBucketRequest.ActionEnum action,
        CcdUpdatePermissionByBucketRequest.PermissionEnum permission,
        CcdUpdatePermissionByBucketRequest.RoleEnum role,
        CancellationToken cancellationToken = default);

    Task<CcdPromoteBucketAsync200Response> PromoteBucketEnvAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string fromRelease,
        string notes,
        string toBucket,
        string toEnvironment,
        CancellationToken cancellationToken = default);

    Task<CcdGetPromotions200ResponseInner?> GetPromotionAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string promotionId,
        CancellationToken cancellationToken = default);

    Task<string> GetBucketIdByName(
        string projectId,
        string environmentId,
        string bucketName,
        CancellationToken cancellationToken);
}
