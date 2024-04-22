using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

class ReleaseClient : IReleaseClient
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IContentDeliveryValidator m_ContentDeliveryValidator;
    readonly IReleasesApi m_ReleasesApi;

    public ReleaseClient(
        IServiceAccountAuthenticationService authenticationService,
        IReleasesApi releasesApi,
        IContentDeliveryValidator contentDeliveryValidator)
    {
        m_AuthenticationService = authenticationService;
        m_ContentDeliveryValidator = contentDeliveryValidator;
        m_ReleasesApi = releasesApi;
    }

    public async Task<CcdGetBucket200ResponseLastRelease> CreateReleaseAsync(
        string projectId,
        string environmentId,
        string bucketId,
        CcdCreateReleaseRequest ccdRelease,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_ReleasesApi.CreateReleaseEnvAsync(
            environmentId,
            bucketId,
            projectId,
            ccdRelease,
            0,
            cancellationToken);
        return response;
    }

    public async Task<CcdGetBucket200ResponseLastRelease> UpdateReleaseAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int releaseNum,
        string notes,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var releaseId = await GetReleaseIdByNumber(
            projectId,
            environmentId,
            bucketId,
            releaseNum,
            cancellationToken);

        var ccdRelease = new CcdUpdateReleaseRequest(notes);
        var response = await m_ReleasesApi.UpdateReleaseEnvAsync(
            environmentId,
            bucketId,
            releaseId,
            projectId,
            ccdRelease,
            0,
            cancellationToken);
        return response;
    }

    public async Task<CcdGetBucket200ResponseLastRelease> GetReleaseAsync(
        string projectId,
        string environmentId,
        string bucketId,
        int releaseNum,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var releaseId = await GetReleaseIdByNumber(
            projectId,
            environmentId,
            bucketId,
            releaseNum,
            cancellationToken);

        var response = await m_ReleasesApi.GetReleaseEnvAsync(
            environmentId,
            bucketId,
            releaseId,
            projectId,
            0,
            cancellationToken);
        return response;
    }

    public async Task<CcdGetBucket200ResponseLastRelease> GetReleaseByBadgeNameAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string badgeName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_ReleasesApi.GetReleaseByBadgeEnvAsync(
            environmentId,
            bucketId,
            badgeName,
            projectId,
            0,
            cancellationToken);
        return response;
    }

    public async Task<ApiResponse<List<CcdGetBucket200ResponseLastRelease>>> ListReleaseAsync(
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
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_ReleasesApi.GetReleasesEnvWithHttpInfoAsync(
            environmentId,
            bucketId,
            projectId,
            page,
            perPage,
            releaseNum,
            notes,
            promotedFromBucket,
            promotedFromRelease,
            badges,
            sortBy,
            sortOrder,
            0,
            cancellationToken);
    }

    public async Task<string> GetReleaseIdByNumber(
        string projectId,
        string environmentId,
        string bucketId,
        int releaseNum,
        CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var result = await m_ReleasesApi.GetReleasesEnvAsync(
            environmentId,
            bucketId,
            projectId,
            1,
            1,
            releaseNum.ToString(),
            null,
            null,
            null,
            null,
            null,
            null,
            0,
            cancellationToken);

        if (result.Count == 0)
            throw new CliException($"No release exists with the number: {releaseNum}", ExitCode.HandledError);

        return result[0].Releaseid.ToString();
    }

    public async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_ReleasesApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }
}
