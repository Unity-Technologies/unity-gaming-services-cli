using System.IO.Abstractions;
using Unity.Services.Cli.CloudContentDelivery.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudContentDelivery.Service;

class EntryClient : IEntryClient
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IContentDeliveryValidator m_ContentDeliveryValidator;
    readonly IEntriesApi m_EntriesApi;
    readonly IContentApi m_ContentApi;
    readonly IFileSystem m_FileSystem;
    readonly IUploadContentClient m_UploadContentClient;

    public EntryClient(
        IServiceAccountAuthenticationService authenticationService,
        IContentDeliveryValidator contentDeliveryValidator,
        IEntriesApi entriesApi,
        IContentApi contentApi,
        IUploadContentClient uploadContentClient,
        IFileSystem fileSystem)
    {
        m_AuthenticationService = authenticationService;
        m_ContentDeliveryValidator = contentDeliveryValidator;
        m_EntriesApi = entriesApi;
        m_ContentApi = contentApi;
        m_UploadContentClient = uploadContentClient;
        m_FileSystem = fileSystem;
    }

    public async Task<CcdCreateOrUpdateEntryBatch200ResponseInner?> UpdateEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string entryPath,
        string versionId,
        List<string>? labels,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var ccdUpdateEntryRequest = new CcdUpdateEntryRequest();

        if (labels != null)
            ccdUpdateEntryRequest.Labels = labels;
        if (metadata != null)
            ccdUpdateEntryRequest.Metadata = CcdUtils.ParseMetadata(metadata)!;

        var responseEntry = await m_EntriesApi.GetEntryByPathEnvAsync(
            environmentId,
            bucketId,
            entryPath,
            projectId,
            versionId,
            0,
            cancellationToken);
        var entryId = responseEntry.Entryid.ToString();

        var response = await m_EntriesApi.UpdateEntryEnvAsync(
            environmentId,
            bucketId,
            entryId,
            projectId,
            ccdUpdateEntryRequest,
            0,
            cancellationToken);
        return response;
    }

    public async Task<Stream> DownloadEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string path,
        string versionId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        m_ContentDeliveryValidator.ValidateBucketId(bucketId);

        var responseEntry = await m_EntriesApi.GetEntryByPathEnvAsync(
            environmentId,
            bucketId,
            path,
            projectId,
            versionId,
            0,
            cancellationToken);
        var entryId = responseEntry.Entryid.ToString();

        var response = await m_ContentApi.GetContentEnvAsync(
            environmentId,
            bucketId,
            entryId,
            projectId,
            versionId,
            0,
            cancellationToken);
        return response;
    }

    public async Task<CcdCreateOrUpdateEntryBatch200ResponseInner?> GetEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string entryPath,
        string versionId,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        m_ContentDeliveryValidator.ValidateBucketId(bucketId);

        return await m_EntriesApi.GetEntryByPathEnvAsync(
            environmentId,
            bucketId,
            entryPath,
            projectId,
            versionId,
            0,
            cancellationToken);

    }

    public async Task<string> DeleteEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string entryPath,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        m_ContentDeliveryValidator.ValidateBucketId(bucketId);

        var response = await m_EntriesApi.GetEntryByPathEnvAsync(
            environmentId,
            bucketId,
            entryPath,
            projectId,
            null,
            0,
            cancellationToken);

        await m_EntriesApi.DeleteEntryEnvAsync(
            environmentId,
            bucketId,
            response.Entryid.ToString(),
            projectId,
            0,
            cancellationToken);
        return "Entry Deleted.";
    }

    public async Task<CcdCreateOrUpdateEntryBatch200ResponseInner?> CopyEntryAsync(
        string projectId,
        string environmentId,
        string bucketId,
        string localPath,
        string remotePath,
        List<string> labels,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        m_ContentDeliveryValidator.ValidateBucketId(bucketId);
        m_ContentDeliveryValidator.ValidatePath(localPath);
        m_ContentDeliveryValidator.ValidatePath(remotePath);

        localPath = CcdUtils.AdjustPathForPlatform(localPath);
        remotePath = CcdUtils.ConvertPathToForwardSlashes(remotePath);

        await using var filestream = m_FileSystem.File.OpenRead(localPath);
        var contentSize = m_UploadContentClient.GetContentSize(filestream);
        var contentType = m_UploadContentClient.GetContentType(localPath);
        var contentHash = m_UploadContentClient.GetContentHash(filestream);
        var entryMetadata = metadata != null ? CcdUtils.ParseMetadata(metadata)! : string.Empty;

        // CREATE OR UPDATE ENTRY
        var ccdCreateOrUpdateEntryByPathRequest =
            new CcdCreateOrUpdateEntryByPathRequest(
                contentHash,
                contentSize,
                contentType,
                labels,
                entryMetadata,
                true);

        var entry = await m_EntriesApi.CreateOrUpdateEntryByPathEnvAsync(
            environmentId,
            bucketId,
            remotePath,
            projectId,
            ccdCreateOrUpdateEntryByPathRequest,
            true,
            0,
            cancellationToken);

        // UPLOAD ENTRY CONTENT
        var response = await m_UploadContentClient.UploadContentToCcd(
            entry.SignedUrl,
            filestream,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new CliException(
                $"There was an error while uploading content to ccd: {response.ReasonPhrase}",
                ExitCode.HandledError);

        // GET CREATED ENTRY
        var response3 = await m_EntriesApi.GetEntryByPathEnvAsync(
            environmentId,
            bucketId,
            remotePath,
            projectId,
            entry.CurrentVersionid.ToString(),
            0,
            cancellationToken);
        return response3;
    }

    public async Task<ApiResponse<List<CcdCreateOrUpdateEntryBatch200ResponseInner>>> ListEntryAsync(
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
        CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        m_ContentDeliveryValidator.ValidateBucketId(bucketId);

        return await m_EntriesApi.GetEntriesEnvWithHttpInfoAsync(
            environmentId,
            bucketId,
            projectId,
            page,
            string.IsNullOrEmpty(startingAfter) ? null : new Guid(startingAfter),
            perPage,
            path,
            label,
            contentType,
            complete,
            sortBy,
            sortOrder,
            0,
            cancellationToken);
    }

    public async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_EntriesApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        m_ContentApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }
}
