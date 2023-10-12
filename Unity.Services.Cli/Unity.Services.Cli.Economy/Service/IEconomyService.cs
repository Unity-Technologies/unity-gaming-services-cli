using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.Service;

interface IEconomyService
{
    public Task<List<GetResourcesResponseResultsInner>> GetResourcesAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    public Task<List<GetResourcesResponseResultsInner>> GetPublishedAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    public Task PublishAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    public Task DeleteAsync(string resourceId, string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    public Task AddAsync(AddConfigResourceRequest request, string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    public Task EditAsync(string resourceId, AddConfigResourceRequest request, string projectId, string environmentId,
        CancellationToken cancellationToken = default);
}
