using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Purchasing.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Api;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Client;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.Authoring;

class CliLiveContentApiTransport : ILiveContentApiTransport
{
    static readonly List<string> k_TaglessVariant = new();

    readonly IConfigsApiAsync m_ConfigsApi;
    readonly IServiceAccountAuthenticationService m_AuthService;
    string m_EnvironmentId = "";
    string m_ProjectId = "";

    public CliLiveContentApiTransport(
        IConfigsApiAsync configsApi,
        IServiceAccountAuthenticationService authService)
    {
        m_ConfigsApi = configsApi;
        m_AuthService = authService;
    }

    public async Task InitializeAsync(
        string environmentId,
        string projectId,
        CancellationToken cancellationToken)
    {
        m_EnvironmentId = environmentId;
        m_ProjectId = projectId;
        await RefreshTokenAsync(cancellationToken);
    }

    public Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsAsync(
        string pathPrefix,
        int limit,
        string after,
        bool? start,
        string schema,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            async () =>
            {
                var response = await m_ConfigsApi.GetConfigsWithHttpInfoAsync(
                    environmentId: m_EnvironmentId,
                    projectId: m_ProjectId,
                    after: after,
                    start: start,
                    limit: limit,
                    path: pathPrefix,
                    schema: schema,
                    variantTag: k_TaglessVariant,
                    cancellationToken: cancellationToken);
                return ToResult(
                    response,
                    IReadOnlyList<LiveContentConfig> (configs) => configs.SelectMany(MapConfig).ToList());
            },
            cancellationToken);
    }

    public Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsContentAsync(
        string pathPrefix,
        int limit,
        string after,
        bool? start,
        string schema,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            async () =>
            {
                var response = await m_ConfigsApi.GetConfigsContentWithHttpInfoAsync(
                    environmentId: m_EnvironmentId,
                    projectId: m_ProjectId,
                    limit: limit,
                    path: pathPrefix,
                    schema: schema,
                    after: after,
                    start: start,
                    variantTag: k_TaglessVariant,
                    cancellationToken: cancellationToken);
                return ToResult(
                    response,
                    IReadOnlyList<LiveContentConfig> (configs) => configs.SelectMany(MapConfigWithContent).ToList());
            },
            cancellationToken);
    }

    public Task<TransportResult<LiveContentConfigBody>> GetConfigContentAsync(
        string path,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            async () =>
            {
                var response = await m_ConfigsApi.GetConfigContentWithHttpInfoAsync(
                    environmentId: m_EnvironmentId,
                    projectId: m_ProjectId,
                    filePath: path,
                    variantTag: k_TaglessVariant,
                    cancellationToken: cancellationToken);
                return ToResult(response, ToBody);
            },
            cancellationToken);
    }

    public Task<TransportResult<LiveContentConfig>> CreateConfigAsync(
        string path,
        string jsonContent,
        CancellationToken cancellationToken)
    {
        return WriteConfigAsync(
            (body, token) => m_ConfigsApi.CreateConfigWithHttpInfoAsync(
                environmentId: m_EnvironmentId,
                projectId: m_ProjectId,
                filePath: path,
                requestBody: body,
                variantTag: k_TaglessVariant,
                cancellationToken: token),
            jsonContent,
            cancellationToken);
    }

    public Task<TransportResult<LiveContentConfig>> UpdateConfigAsync(
        string path,
        string jsonContent,
        CancellationToken cancellationToken)
    {
        return WriteConfigAsync(
            (body, token) => m_ConfigsApi.UpdateConfigWithHttpInfoAsync(
                environmentId: m_EnvironmentId,
                projectId: m_ProjectId,
                filePath: path,
                requestBody: body,
                variantTag: k_TaglessVariant,
                cancellationToken: token),
            jsonContent,
            cancellationToken);
    }

    public Task<TransportResult> DeleteConfigAsync(
        string path,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            async () =>
            {
                var response = await m_ConfigsApi.DeleteConfigWithHttpInfoAsync(
                    environmentId: m_EnvironmentId,
                    projectId: m_ProjectId,
                    filePath: path,
                    variantTag: k_TaglessVariant,
                    cancellationToken: cancellationToken);
                return new TransportResult((int)response.StatusCode, response.ErrorText, ToHeaders(response.Headers));
            },
            cancellationToken);
    }

    async Task<TransportResult<LiveContentConfig>> WriteConfigAsync(
        Func<Dictionary<string, object>, CancellationToken,
            Task<ApiResponse<LcmGetAssets200ResponseInnerOneOf1>>> send,
        string jsonContent,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            async () =>
            {
                var body = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent)
                    ?? new Dictionary<string, object>();
                var response = await send(body, cancellationToken);
                return ToResult(response, MapSingleTaglessConfig);
            },
            cancellationToken);
    }

    async Task<TransportResult<T>> ExecuteAsync<T>(
        Func<Task<TransportResult<T>>> request,
        CancellationToken cancellationToken)
    {
        await RefreshTokenAsync(cancellationToken);
        try
        {
            return await request();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ApiException exception)
        {
            return new TransportResult<T>(
                exception.ErrorCode,
                default!,
                ToHeaders(exception.Headers),
                GetError(exception));
        }
    }

    async Task<TransportResult> ExecuteAsync(
        Func<Task<TransportResult>> request,
        CancellationToken cancellationToken)
    {
        await RefreshTokenAsync(cancellationToken);
        try
        {
            return await request();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ApiException exception)
        {
            return new TransportResult(
                exception.ErrorCode,
                GetError(exception),
                ToHeaders(exception.Headers));
        }
    }

    async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        var token = await m_AuthService.GetAccessTokenAsync(cancellationToken);
        m_ConfigsApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    static TransportResult<TContent> ToResult<TResponse, TContent>(
        ApiResponse<TResponse> response,
        Func<TResponse, TContent> map)
    {
        var isSuccess = (int)response.StatusCode is >= 200 and < 300;
        return new TransportResult<TContent>(
            (int)response.StatusCode,
            isSuccess ? map(response.Data) : default!,
            ToHeaders(response.Headers),
            isSuccess ? null : GetError(response));
    }

    static string GetError(IApiResponse response)
    {
        return string.IsNullOrEmpty(response.ErrorText) ? response.RawContent : response.ErrorText;
    }

    static string GetError(ApiException exception)
    {
        return exception.ErrorContent switch
        {
            null => exception.Message,
            string content => content,
            _ => JsonConvert.SerializeObject(exception.ErrorContent),
        };
    }

    static IReadOnlyDictionary<string, string> ToHeaders(Multimap<string, string>? headers)
    {
        return headers?.ToDictionary(pair => pair.Key, pair => string.Join(",", pair.Value), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    static IEnumerable<LiveContentConfig> MapConfig(LcmGetAssets200ResponseInnerOneOf1 config)
    {
        return config.Variants.Select(variant => new LiveContentConfig(
            config.Id,
            config.Path,
            config.ContentType,
            config.SortIndex,
            config.Schemas,
            variant.VariantTag,
            variant.ContentHash,
            variant.ContentSize,
            variant.Complete,
            variant.CreatedAt,
            variant.UpdatedAt,
            variant.Metadata));
    }

    static IEnumerable<LiveContentConfig> MapConfigWithContent(LcmGetConfigsContent200ResponseInner config)
    {
        return config.Variants.Select(variant => new LiveContentConfig(
            config.Id,
            config.Path,
            config.ContentType,
            config.SortIndex,
            config.Schemas,
            variant.VariantTag,
            variant.ContentHash,
            variant.ContentSize,
            variant.Complete,
            variant.CreatedAt,
            variant.UpdatedAt,
            variant.Metadata,
            ToBody(variant.Content)));
    }

    static LiveContentConfig MapSingleTaglessConfig(LcmGetAssets200ResponseInnerOneOf1 config)
    {
        var taglessVariants = config.Variants.Where(variant => variant.VariantTag.Count == 0).ToList();
        if (taglessVariants.Count != 1)
        {
            throw new PurchasingException(
                $"Expected exactly one tagless variant for config '{config.Path}', but received {taglessVariants.Count}.",
                exitCode: ExitCode.UnhandledError);
        }

        var variant = taglessVariants[0];
        return new LiveContentConfig(
            config.Id,
            config.Path,
            config.ContentType,
            config.SortIndex,
            config.Schemas,
            variant.VariantTag,
            variant.ContentHash,
            variant.ContentSize,
            variant.Complete,
            variant.CreatedAt,
            variant.UpdatedAt,
            variant.Metadata);
    }

    static LiveContentConfigBody ToBody(Dictionary<string, object>? content)
    {
        return new LiveContentConfigBody(JObject.FromObject(content ?? new Dictionary<string, object>()));
    }
}
