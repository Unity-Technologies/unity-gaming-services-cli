using Unity.Services.Gateway.ObservabilityApiV1.Generated.Client;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;

namespace Unity.Services.Cli.Observability.Service;

public interface IObservabilityService
{
    Task<ApiResponse<LogsResponse>> GetLogsAsync(
        string projectId,
        string environmentId,
        string? from,
        string? to,
        string? query,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default);
}
