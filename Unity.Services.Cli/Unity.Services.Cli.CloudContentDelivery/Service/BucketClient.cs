using System.Net;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

public class BucketClient : IBucketClient
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IBucketsApi m_BucketsApi;
    readonly IContentDeliveryValidator m_ContentDeliveryValidator;
    readonly IPermissionsApi m_PermissionsApi;

    public BucketClient(
        IServiceAccountAuthenticationService authenticationService,
        IBucketsApi bucketsApi,
        IPermissionsApi permissionsApi,
        IContentDeliveryValidator contentDeliveryValidator)
    {
        m_AuthenticationService = authenticationService;
        m_ContentDeliveryValidator = contentDeliveryValidator;
        m_BucketsApi = bucketsApi;
        m_PermissionsApi = permissionsApi;
    }

    public async
        Task<ApiResponse<List<CcdGetBucket200Response>>> ListBucketAsync(
            string projectId,
            string environmentId,
            int? page,
            int? perPage,
            string filterName,
            string description,
            string sortBy,
            string sortOrder,
            CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_BucketsApi.ListBucketsByProjectEnvWithHttpInfoAsync(
            environmentId,
            projectId,
            page,
            perPage,
            filterName,
            description,
            sortBy,
            sortOrder,
            0,
            cancellationToken);
    }


    public async Task<CcdGetBucket200Response?> GetBucketAsync(
        string projectId,
        string environmentId,
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var bucketId = await GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        var response = await m_BucketsApi.GetBucketEnvAsync(
            environmentId,
            bucketId,
            projectId,
            0,
            cancellationToken);
        return response;
    }

    public async Task<CcdGetBucket200Response?> CreateBucketAsync(
        string projectId,
        string environmentId,
        string name,
        string description,
        bool isPrivate,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var
            ccdBucket =
                new CcdCreateBucketByProjectRequest(
                    name: name,
                    projectguid: Guid.Parse(projectId),
                    description: description,
                    _private: isPrivate
                );
        var response = await m_BucketsApi.CreateBucketByProjectEnvAsync(
            environmentId,
            projectId,
            ccdBucket,
            0,
            cancellationToken);
        return response;
    }

    public async Task<object> DeleteBucketAsync(
        string projectId,
        string environmentId,
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var bucketId = await GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        await m_BucketsApi.DeleteBucketEnvAsync(
            environmentId,
            bucketId,
            projectId,
            0,
            cancellationToken);

        return "Bucket deleted.";
    }

    public async Task<CcdGetAllByBucket200ResponseInner> UpdatePermissionBucketAsync(
        string projectId,
        string environmentId,
        string bucketName,
        CcdUpdatePermissionByBucketRequest.ActionEnum action,
        CcdUpdatePermissionByBucketRequest.PermissionEnum permission,
        CcdUpdatePermissionByBucketRequest.RoleEnum role,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var bucketId = await GetBucketIdByName(
            projectId,
            environmentId,
            bucketName,
            cancellationToken);

        var ccdUpdatePermissionByBucketRequest = new CcdUpdatePermissionByBucketRequest(
            action,
            permission,
            role);
        var permissionList = await m_PermissionsApi.GetAllByBucketEnvAsync(
            environmentId,
            bucketId,
            projectId,
            0,
            cancellationToken);

        // Problem: Due to permissions list result not being typed, (we receive a string instead of an enum)
        // we need to compare the strings as they are serialized against what we receive
        var actionStr = JsonConvert.SerializeObject(action).Replace("\"","");
        var roleStr = JsonConvert.SerializeObject(role).Replace("\"","");

        var writePermissions = permissionList
            .Find(x =>
                x.Action == actionStr
                    && x.Role == roleStr);

        if (writePermissions != null)
            return await m_PermissionsApi.UpdatePermissionByBucketEnvAsync(
                environmentId,
                bucketId,
                projectId,
                ccdUpdatePermissionByBucketRequest,
                0,
                cancellationToken);
        return await m_PermissionsApi.CreatePermissionByBucketEnvAsync(
            environmentId,
            bucketId,
            projectId,
            ccdUpdatePermissionByBucketRequest,
            0,
            cancellationToken);
    }

    public async Task<CcdPromoteBucketAsync200Response> PromoteBucketEnvAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string fromRelease,
        string notes,
        string toBucket,
        string toEnvironment,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var promoteBucketRequest = new CcdPromoteBucketRequest(
            new Guid(fromRelease),
            notes,
            new Guid(toBucket),
            new Guid(toEnvironment));

        return await m_BucketsApi.PromoteBucketAsyncEnvAsync(
            environmentId,
            bucketId,
            projectId,
            promoteBucketRequest,
            0,
            cancellationToken);
    }

    public async Task<CcdGetPromotions200ResponseInner?> GetPromotionAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string promotionId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        return await m_BucketsApi.GetPromotionEnvAsync(
            environmentId,
            bucketId,
            promotionId,
            projectId,
            0,
            cancellationToken);
    }

    public async Task<string> GetBucketIdByName(
        string projectId,
        string environmentId,
        string bucketName,
        CancellationToken cancellationToken)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var page = 1;
        const int perPage = 100;

        while (true)
        {
            try
            {
                var result = await m_BucketsApi.ListBucketsByProjectEnvWithHttpInfoAsync(
                    environmentId,
                    projectId,
                    page,
                    perPage,
                    bucketName,
                    null,
                    null,
                    null,
                    0,
                    cancellationToken);

                var exactMatch = result.Data.FirstOrDefault(
                    bucket => string.Equals(bucket.Name, bucketName, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null) return exactMatch.Id.ToString();

                if (result.StatusCode != HttpStatusCode.OK || !result.Data.Any()) break;

            }
            catch (ApiException ex) when (ex.ErrorCode == (int)HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                break;
            }
            catch (Exception ex)
            {
                throw new CliException(
                    $"A server error occured while fetching the bucket: {ex.Message}, please try again.",
                    ExitCode.HandledError);
            }

            page++;
        }

        throw new CliException($"No bucket exists with the name: {bucketName}", ExitCode.HandledError);

    }

    internal async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_BucketsApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        m_PermissionsApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }
}
