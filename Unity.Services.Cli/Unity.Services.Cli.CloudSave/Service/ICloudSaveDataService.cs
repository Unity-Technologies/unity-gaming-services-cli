using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudSave.Service;

interface ICloudSaveDataService
{
    public Task<GetIndexIdsResponse> ListIndexesAsync(string projectId, string environmentId, CancellationToken cancellationToken = default);
    public Task<CreateIndexResponse> CreateCustomIndexAsync(string projectId, string environmentId, string? fields, string? visibility, string? body, CancellationToken cancellationToken = default);
    public Task<QueryIndexResponse> QueryPlayerDataAsync(string projectId, string environmentId, string? visibility, string body, CancellationToken cancellationToken = default);
    public Task<QueryIndexResponse> QueryCustomDataAsync(string projectId, string environmentId, string? visibility, string body, CancellationToken cancellationToken = default);
    public Task<CreateIndexResponse> CreatePlayerIndexAsync(string projectId, string environmentId, string? fields, string? visibility, string? body, CancellationToken cancellationToken = default);
    public Task<GetCustomIdsResponse> ListCustomDataIdsAsync(string projectId, string environmentId, string? start, int? limit, CancellationToken cancellationToken = default);
    public Task<GetPlayersWithDataResponse> ListPlayerDataIdsAsync(string projectId, string environmentId, string? start, int? limit, CancellationToken cancellationToken = default);
    public Task<SetItemResponse> SetPlayerDataItemAsync(string projectId, string environmentId, string? playerId, string? key, object? value, string? writeLock, string? visibility, CancellationToken cancellationToken = default);
    public Task<SetItemResponse> SetCustomDataItemAsync(string projectId, string environmentId, string? customId, string? key, object? value, string? writeLock, string? visibility, CancellationToken cancellationToken = default);
    public Task<GetItemsResponse> GetPlayerDataItemsAsync(string projectId, string environmentId, string? playerId, List<string>? keys, string? after, string? visibility, CancellationToken cancellationToken = default);
    public Task<GetItemsResponse> GetCustomDataItemsAsync(string projectId, string environmentId, string? customId, List<string>? keys, string? after, string? visibility, CancellationToken cancellationToken = default);
}
