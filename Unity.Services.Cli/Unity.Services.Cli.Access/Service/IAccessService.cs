using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.Service;

public interface IAccessService
{
    Task<Policy> GetPolicyAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    Task<PlayerPolicy> GetPlayerPolicyAsync(string projectId, string environmentId, string playerId,
        CancellationToken cancellationToken = default);

    Task<List<PlayerPolicy>> GetAllPlayerPoliciesAsync(string projectId, string environmentId,
        CancellationToken cancellationToken = default);

    Task UpsertPolicyAsync(string projectId, string environmentId, FileInfo file,
        CancellationToken cancellationToken = default);

    Task UpsertPlayerPolicyAsync(string projectId, string environmentId, string playerId, FileInfo file,
        CancellationToken cancellationToken = default);

    Task DeletePolicyStatementsAsync(string projectId, string environmentId, FileInfo file,
        CancellationToken cancellationToken = default);

    Task DeletePlayerPolicyStatementsAsync(string projectId, string environmentId, string playerId, FileInfo file,
        CancellationToken cancellationToken = default);

    Task UpsertProjectAccessCaCAsync(string projectId, string environmentId, Policy policy,
        CancellationToken cancellationToken = default);

    Task DeleteProjectAccessCaCAsync(string projectId, string environmentId, DeleteOptions options,
        CancellationToken cancellationToken = default);
}
