using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Service;

interface ITriggersService
{
    Task<IEnumerable<TriggerConfig>> GetTriggersAsync(
        string projectId,
        string environmentId,
        int? limit,
        CancellationToken cancellationToken = default);

    Task CreateTriggerAsync(
        string projectId,
        string environmentId,
        TriggerConfigBody config,
        CancellationToken cancellationToken = default);

    Task UpdateTriggerAsync(
        string projectId,
        string environmentId,
        string triggerId,
        TriggerConfigBody config,
        CancellationToken cancellationToken = default);

    Task DeleteTriggerAsync(
        string projectId,
        string environmentId,
        string id,
        CancellationToken cancellationToken = default);

    public Task<string> GetRequestAsync(string? address, CancellationToken cancellationToken = default);

    public Task WriteToFileAsync(string outputFile, string result, CancellationToken cancellationToken = default);
}
