using Newtonsoft.Json;
using Unity.Services.Cli.CloudSave.Utils;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Api;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudSave.Service;

class CloudSaveDataService : ICloudSaveDataService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly IDataApiAsync m_DataApiAsync;
    readonly IConfigurationValidator m_ConfigValidator;

    public CloudSaveDataService(IDataApiAsync dataApiAsync, IConfigurationValidator validator,
        IServiceAccountAuthenticationService authenticationService)
    {
        m_DataApiAsync = dataApiAsync;
        m_ConfigValidator = validator;
        m_AuthenticationService = authenticationService;
    }

    public async Task<GetIndexIdsResponse> ListIndexesAsync(string projectId, string environmentId, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_DataApiAsync.ListIndexesAsync(
            projectId: Guid.Parse(projectId),
            environmentId: Guid.Parse(environmentId),
            cancellationToken: cancellationToken);

        return response;
    }

    public async Task<CreateIndexResponse> CreateCustomIndexAsync(string projectId, string environmentId, string? fields, string? visibility, string? body, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var createIndexBody = GetCreateIndexBody(fields, body);

        return visibility switch
        {
            CustomIndexVisibilityTypes.Private => await m_DataApiAsync.CreatePrivateCustomIndexAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                createIndexBody: createIndexBody,
                cancellationToken: cancellationToken),
            _ => await m_DataApiAsync.CreateDefaultCustomIndexAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                createIndexBody: createIndexBody,
                cancellationToken: cancellationToken)
        };
    }

    public async Task<QueryIndexResponse> QueryPlayerDataAsync(string projectId, string environmentId, string? visibility, string? body, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var queryIndexBody = !string.IsNullOrEmpty(body) ? DeserializeOrThrow<QueryIndexBody>(body) : new QueryIndexBody();

        return visibility switch
        {
            PlayerIndexVisibilityTypes.Public => await m_DataApiAsync.QueryPublicPlayerDataAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                queryIndexBody: queryIndexBody,
                cancellationToken: cancellationToken),
            PlayerIndexVisibilityTypes.Protected => await m_DataApiAsync.QueryProtectedPlayerDataAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                queryIndexBody: queryIndexBody,
                cancellationToken: cancellationToken),
            _ => await m_DataApiAsync.QueryDefaultPlayerDataAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                queryIndexBody: queryIndexBody,
                cancellationToken: cancellationToken)
        };
    }

    public async Task<QueryIndexResponse> QueryCustomDataAsync(string projectId, string environmentId, string? visibility, string? body, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var queryIndexBody = !string.IsNullOrEmpty(body) ? DeserializeOrThrow<QueryIndexBody>(body) : new QueryIndexBody();

        return visibility switch
        {
            CustomIndexVisibilityTypes.Private => await m_DataApiAsync.QueryPrivateCustomDataAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                queryIndexBody: queryIndexBody,
                cancellationToken: cancellationToken),
            _ => await m_DataApiAsync.QueryDefaultCustomDataAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                queryIndexBody: queryIndexBody,
                cancellationToken: cancellationToken)
        };
    }

    public async Task<CreateIndexResponse> CreatePlayerIndexAsync(string projectId, string environmentId, string? fields, string? visibility, string? body, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);
        var createIndexBody = GetCreateIndexBody(fields, body);

        return visibility switch
        {
            PlayerIndexVisibilityTypes.Public => await m_DataApiAsync.CreatePublicPlayerIndexAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                createIndexBody: createIndexBody,
                cancellationToken: cancellationToken),
            PlayerIndexVisibilityTypes.Protected => await m_DataApiAsync.CreateProtectedPlayerIndexAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                createIndexBody: createIndexBody,
                cancellationToken: cancellationToken),
            _ => await m_DataApiAsync.CreateDefaultPlayerIndexAsync(
                projectId: Guid.Parse(projectId),
                environmentId: Guid.Parse(environmentId),
                createIndexBody: createIndexBody,
                cancellationToken: cancellationToken)
        };
    }

    public async Task<GetCustomIdsResponse> ListCustomDataIdsAsync(string projectId, string environmentId, string? start, int? limit, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_DataApiAsync.ListCustomDataIDsAsync(
            projectId: Guid.Parse(projectId),
            environmentId: Guid.Parse(environmentId),
            start: start,
            limit: limit,
            cancellationToken: cancellationToken);

        return response;
    }

    public async Task<GetPlayersWithDataResponse> ListPlayerDataIdsAsync(string projectId, string environmentId, string? start, int? limit, CancellationToken cancellationToken = default)
    {
        await AuthorizeServiceAsync(cancellationToken);
        ValidateProjectIdAndEnvironmentId(projectId, environmentId);

        var response = await m_DataApiAsync.GetPlayersWithItemsAsync(
            projectId: Guid.Parse(projectId),
            environmentId: Guid.Parse(environmentId),
            start: start,
            limit: limit,
            cancellationToken: cancellationToken);

        return response;
    }

    static CreateIndexBody GetCreateIndexBody(string? fields, string? body)
    {
        if (!string.IsNullOrEmpty(fields) && !string.IsNullOrEmpty(body))
        {
            throw new CliException($"Index body and fields cannot both be specified.", ExitCode.HandledError);
        }

        if (!string.IsNullOrEmpty(fields))
        {
            var fieldsObject = DeserializeOrThrow<List<IndexField>>(fields);
            return new CreateIndexBody(new CreateIndexBodyIndexConfig(fieldsObject));
        }

        if (!string.IsNullOrEmpty(body))
        {
            var bodyObject = DeserializeOrThrow<CreateIndexBody>(body);
            return bodyObject ?? new CreateIndexBody();
        }

        throw new CliException($"Index body or fields is required.", ExitCode.HandledError);
    }

    internal async Task AuthorizeServiceAsync(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        m_DataApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    internal void ValidateProjectIdAndEnvironmentId(string projectId, string environmentId)
    {
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, projectId);
        m_ConfigValidator.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, environmentId);
    }

    /// <summary>
    /// Helper function to wrap JSON deserialization in order to throw a <see cref="CliException"/> for errors.
    /// </summary>
    static T? DeserializeOrThrow<T>(string value)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (Exception ex)
        {
            throw new CliException($"Failed to deserialize object for Cloud Save request. ({ex.Message})", ex, ExitCode.HandledError);
        }
    }
}
