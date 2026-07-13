using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Purchasing.Model;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Api;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Client;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.Authoring;

class PurchasingClient : ILiveContentConfigClient
{
    const int k_MaxConcurrentFetches = 8;
    const int k_PageSize = 100;
    const string k_MetadataKey = "$metadata";
    const string k_ManagedByKey = "managedBy";
    const string k_ManagedByValue = "In App Purchase";

    static readonly JsonSerializerSettings k_DtoSettings = new()
    {
        Converters = { new StringEnumConverter() },
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
    };

    static readonly JsonSerializer k_DtoSerializer = JsonSerializer.Create(k_DtoSettings);

    readonly IConfigsApiAsync m_ConfigsApi;
    readonly IServiceAccountAuthenticationService m_AuthService;

    string m_EnvironmentId = "";
    string m_ProjectId = "";

    public PurchasingClient(
        IConfigsApiAsync configsApi,
        IServiceAccountAuthenticationService authService)
    {
        m_ConfigsApi = configsApi;
        m_AuthService = authService;
    }

    async Task AuthorizeAsync(CancellationToken cancellationToken)
    {
        var token = await m_AuthService.GetAccessTokenAsync(cancellationToken);
        m_ConfigsApi.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
    }

    public async Task Initialize(
        string environmentId,
        string projectId,
        CancellationToken cancellationToken)
    {
        await AuthorizeAsync(cancellationToken);
        m_EnvironmentId = environmentId;
        m_ProjectId = projectId;
    }

    public async Task<List<CatalogItem>> List(CancellationToken cancellationToken)
    {
        await AuthorizeAsync(cancellationToken);
        var configPaths = await FetchAllConfigPaths(cancellationToken);

        using var gate = new SemaphoreSlim(k_MaxConcurrentFetches);
        var fetchTasks = configPaths
            .Select(path => FetchConfigContent(path, gate, cancellationToken))
            .ToList();

        var contents = await Task.WhenAll(fetchTasks);

        var result = new List<CatalogItem>();
        for (var i = 0; i < contents.Length; i++)
        {
            var content = contents[i];
            if (content == null)
                continue;

            try
            {
                var json = JsonConvert.SerializeObject(content);
                var dto = JsonConvert.DeserializeObject<CatalogItemDto>(json, k_DtoSettings);
                if (dto == null || string.IsNullOrEmpty(dto.uSku))
                    continue;

                var item = CatalogItemDtoConverter.ConvertFromDto(dto);
                item.CatalogListingId = configPaths[i];
                result.Add(item);
            }
            catch (Exception)
            {
                // Skip items that fail to deserialize
            }
        }

        return result;
    }

    public async Task Upsert(CatalogItem catalogItem, CancellationToken cancellationToken)
    {
        await AuthorizeAsync(cancellationToken);
        var path = catalogItem.CatalogListingId;
        var dto = CatalogItemDtoConverter.ConvertToDto(catalogItem);
        var body = JObject.FromObject(dto, k_DtoSerializer);

        Dictionary<string, object>? existingContent = null;
        try
        {
            existingContent = await m_ConfigsApi.GetConfigContentAsync(
                projectId: m_ProjectId,
                environmentId: m_EnvironmentId,
                filePath: path,
                cancellationToken: cancellationToken);
        }
        catch (ApiException ex) when (ex.ErrorCode == 404)
        {
            // Config does not exist — will be created below
        }

        string? existingJson = existingContent != null
            ? JsonConvert.SerializeObject(existingContent)
            : null;

        AddManagedByMetadata(body, existingJson);

        var requestBody = body.ToObject<Dictionary<string, object>>()!;

        try
        {
            if (existingContent != null)
            {
                await m_ConfigsApi.UpdateConfigFileAsync(
                    projectId: m_ProjectId,
                    environmentId: m_EnvironmentId,
                    filePath: path,
                    requestBody: requestBody,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await m_ConfigsApi.CreateConfigFileAsync(
                    projectId: m_ProjectId,
                    environmentId: m_EnvironmentId,
                    filePath: path,
                    requestBody: requestBody,
                    cancellationToken: cancellationToken);
            }
        }
        catch (ApiException ex)
        {
            var verb = existingContent != null ? "update" : "create";
            throw new ClientException(
                $"Failed to {verb} config at '{path}' (HTTP {ex.ErrorCode}). {ex.Message}", null);
        }
    }

    public async Task Delete(CatalogItem catalogItem, CancellationToken cancellationToken)
    {
        await AuthorizeAsync(cancellationToken);
        var path = catalogItem.CatalogListingId;

        try
        {
            await m_ConfigsApi.DeleteConfigAsync(
                projectId: m_ProjectId,
                environmentId: m_EnvironmentId,
                filePath: path,
                cancellationToken: cancellationToken);
        }
        catch (ApiException ex)
        {
            throw new ClientException(
                $"Failed to delete config at '{path}' (HTTP {ex.ErrorCode}). {ex.Message}", null);
        }
    }

    async Task<List<string>> FetchAllConfigPaths(CancellationToken cancellationToken)
    {
        var configPaths = new List<string>();
        string? afterCursor = null;
        var isFirstPage = true;

        while (true)
        {
            List<Unity.Services.Gateway.LiveContentApiV1.Generated.Model.LcmGetAssets200ResponseInner> page;
            try
            {
                page = await m_ConfigsApi.GetConfigsAsync(
                    projectId: m_ProjectId,
                    environmentId: m_EnvironmentId,
                    path: "catalog/",
                    limit: k_PageSize,
                    schema: CatalogItemDtoConverter.RequiredSchema,
                    noVariantTag: true,
                    after: afterCursor,
                    start: isFirstPage ? true : null,
                    cancellationToken: cancellationToken);
            }
            catch (ApiException ex) when (ex.ErrorCode == 404)
            {
                break;
            }

            isFirstPage = false;

            var paths = page
                .Select(r => r.Path)
                .Where(p => !string.IsNullOrEmpty(p))
                .Cast<string>()
                .ToList();

            configPaths.AddRange(paths);

            if (paths.Count < k_PageSize)
                break;

            afterCursor = paths.LastOrDefault();
        }

        return configPaths;
    }

    async Task<Dictionary<string, object>?> FetchConfigContent(
        string configPath, SemaphoreSlim gate, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await m_ConfigsApi.GetConfigContentAsync(
                projectId: m_ProjectId,
                environmentId: m_EnvironmentId,
                filePath: configPath,
                cancellationToken: cancellationToken);
        }
        catch (ApiException ex) when (ex.ErrorCode == 404)
        {
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    static void AddManagedByMetadata(JObject body, string? existingJson)
    {
        JObject? metadata = null;

        if (!string.IsNullOrEmpty(existingJson))
        {
            try
            {
                var existingJObject = JObject.Parse(existingJson);
                metadata = existingJObject[k_MetadataKey] as JObject;
            }
            catch (JsonException)
            {
                // Fallback to empty metadata
            }
        }

        metadata ??= new JObject();
        metadata[k_ManagedByKey] = k_ManagedByValue;
        body[k_MetadataKey] = metadata;
    }

}
