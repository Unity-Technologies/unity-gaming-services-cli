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
}
