using Unity.Services.Gateway.PlayerAuthApiV1.Generated.Model;

namespace Unity.Services.Cli.Player.Service;

public interface IPlayerService
{
    public Task DeleteAsync(string projectId, string playerId, CancellationToken cancellationToken = default);

    public Task EnableAsync(string projectId, string playerId, CancellationToken cancellationToken = default);
    
    public Task DisableAsync(string projectId, string playerId, CancellationToken cancellationToken = default);
    public Task<PlayerAuthAuthenticationResponse> CreateAsync(string projectId, CancellationToken cancellationToken = default);
}
