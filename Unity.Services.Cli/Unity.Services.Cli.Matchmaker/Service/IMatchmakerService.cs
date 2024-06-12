using Unity.Services.Matchmaker.Authoring.Core.Model;
using EnvironmentConfig = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model.EnvironmentConfig;
using QueueConfig = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model.QueueConfig;

namespace Unity.Services.Cli.Matchmaker.Service;

public interface IMatchmakerService
{
    Task<string> Initialize(string projectId, string environmentId, CancellationToken ct = default);

    Task<(bool, EnvironmentConfig)> GetEnvironmentConfig(CancellationToken ct = default);

    Task<List<ErrorResponse>> UpsertEnvironmentConfig(EnvironmentConfig environmentConfig, bool dryRun, CancellationToken ct = default);

    Task<List<QueueConfig>> ListQueues(CancellationToken ct = default);

    Task<List<ErrorResponse>> UpsertQueueConfig(QueueConfig queueConfig, bool dryRun, CancellationToken ct = default);

    Task DeleteQueue(string queueName, bool dryRun, CancellationToken ct = default);
}
