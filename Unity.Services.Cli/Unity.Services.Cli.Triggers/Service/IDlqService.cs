using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.Service;

interface IDlqService
{
    Task<List<DLQEvent>> ListDlqEventsAsync(
        string projectId,
        string environmentId,
        int? limit,
        string? status,
        DateTime? createdFrom,
        DateTime? createdTo,
        string? resolutionAction,
        Guid? eventId,
        CancellationToken cancellationToken = default);

    Task<DLQEvent> GetDlqEventAsync(
        string projectId,
        string environmentId,
        string eventId,
        CancellationToken cancellationToken = default);

    Task ReplayDlqEventAsync(
        string projectId,
        string environmentId,
        string eventId,
        CancellationToken cancellationToken = default);

    Task DiscardDlqEventAsync(
        string projectId,
        string environmentId,
        string eventId,
        CancellationToken cancellationToken = default);

    Task<DLQQueuedResult> ReplayAllDlqEventsAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken = default);

    Task<DLQDiscardResult> DiscardAllDlqEventsAsync(
        string projectId,
        string environmentId,
        CancellationToken cancellationToken = default);
}
