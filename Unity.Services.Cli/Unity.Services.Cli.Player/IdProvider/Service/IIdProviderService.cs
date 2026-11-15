using Unity.Services.Cli.Player.IdProvider.Models;

namespace Unity.Services.Cli.Player.IdProvider.Service;

/// <summary>
/// Identity provider administration over the player-identity v2 admin API, which addresses each
/// configuration by its server-assigned configId and accepts a user login or a service account.
/// </summary>
public interface IIdProviderService
{
    /// <param name="type">Only configurations of this provider type; all of them when null.</param>
    Task<IReadOnlyList<IdProviderResponse>> ListAsync(string projectId, string? type = null, CancellationToken cancellationToken = default);

    Task<IdProviderResponse> GetAsync(string projectId, string configId, CancellationToken cancellationToken = default);

    Task<IdProviderResponse> CreateAsync(string projectId, CreateIdProviderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change only the fields set on <paramref name="request"/>.
    /// </summary>
    Task<IdProviderResponse> UpdateAsync(string projectId, string configId, UpdateIdProviderRequest request, CancellationToken cancellationToken = default);

    Task<IdProviderResponse> EnableAsync(string projectId, string configId, CancellationToken cancellationToken = default);

    Task<IdProviderResponse> DisableAsync(string projectId, string configId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string projectId, string configId, CancellationToken cancellationToken = default);
}
