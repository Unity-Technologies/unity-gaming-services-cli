using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public class BadgeClient : IBadgeClient
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IBadgesApi m_BadgesApi;
    readonly IContentDeliveryValidator m_ContentDeliveryValidator;

    public BadgeClient(
        IServiceAccountAuthenticationService authenticationService,
        IBadgesApi badgesApi,
        IContentDeliveryValidator contentDeliveryValidator)
    {
        m_AuthenticationService = authenticationService;
        m_ContentDeliveryValidator = contentDeliveryValidator;
        m_BadgesApi = badgesApi;
    }

    public async Task<ApiResponse<List<CcdGetBucket200ResponseLastReleaseBadgesInner>>> ListBadgeAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int? page,
        int? perPage,
        string filterName,
        string releaseNum,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_BadgesApi.ListBadgesEnvWithHttpInfoAsync(
            environmentId,
            bucketId,
            projectId,
            page,
            perPage,
            filterName,
            releaseNum,
            sortBy,
            sortOrder,
            0,
            cancellationToken);
    }

    public async Task<CcdGetBucket200ResponseLastReleaseBadgesInner> CreateBadgeAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string badgeName,
        long releaseNum,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var releaseNumber = releaseNum;

        var ccdUpdateBadgeRequest =
            new CcdUpdateBadgeRequest(badgeName, default, releaseNumber);
        return await m_BadgesApi.UpdateBadgeEnvAsync(
            environmentId,
            bucketId,
            projectId,
            ccdUpdateBadgeRequest,
            0,
            cancellationToken);
    }

    public async Task<string> DeleteBadgeAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string badgeName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        m_ContentDeliveryValidator.ValidateBucketId(bucketId);

        await m_BadgesApi.DeleteBadgeEnvAsync(
            environmentId,
            bucketId,
            badgeName,
            projectId,
            0,
            cancellationToken);
        return "Badge deleted.";
    }

    public async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_BadgesApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }
}
