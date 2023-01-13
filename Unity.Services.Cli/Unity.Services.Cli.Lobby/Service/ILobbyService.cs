using Unity.Services.MpsLobby.LobbyApiV1.Generated.Model;
using static Unity.Services.MpsLobby.LobbyApiV1.Generated.Model.TokenRequest;
using LobbyModel = Unity.Services.MpsLobby.LobbyApiV1.Generated.Model.Lobby;

namespace Unity.Services.Cli.Lobby.Service;

public interface ILobbyService
{
    /// <summary>
    /// Bulk update a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="body">A string containing the request body in JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> BulkUpdateLobbyAsync(string? projectId, string? environmentId, string? serviceId, string lobbyId, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="body">A string containing the request body in JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> CreateLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="lobbyId">The ID of the lobby to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task DeleteLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken);

    /// <summary>
    /// Get the authenticated player's joined lobbies.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<IEnumerable<string>> GetJoinedLobbiesAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, CancellationToken cancellationToken);

    /// <summary>
    /// Get the authenticated player's hosted lobbies.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<IEnumerable<string>> GetHostedLobbiesAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, CancellationToken cancellationToken);

    /// <summary>
    /// Get a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="lobbyId">The ID of the lobby to get.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> GetLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken);

    /// <summary>
    /// Join a lobby by ID or code.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="lobbyId">A nullable string containing the lobby ID.</param>
    /// <param name="lobbyCode">A nullable string containing the lobby code.</param>
    /// <param name="player">A string containing the joining player's details in JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> JoinLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? lobbyId, string? lobbyCode, string player, CancellationToken cancellationToken);

    /// <summary>
    /// Reconnect to a lobby as the authenticated player.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="lobbyId">The ID of the lobby to reconnect to.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> ReconnectAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken);

    /// <summary>
    /// Query lobbies.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="body">A nullable string containing the request body in JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<QueryResponse> QueryLobbiesAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string? body, CancellationToken cancellationToken);

    /// <summary>
    /// Quick-join a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="filter">A string containing the QuickJoin request filter in JSON.</param>
    /// <param name="player">A string containing the joining player's details in JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> QuickJoinAsync(string? projectId, string? environmentId, string? serviceId, string filter, string player, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a player from a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="playerId">The ID of the player to remove from the lobby.</param>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="playerId">The ID of the player.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task RemovePlayerAsync(string? projectId, string? environmentId, string? serviceId, string? playerId, string lobbyId, CancellationToken cancellationToken);

    /// <summary>
    /// Update a player in a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="playerId">The ID of the player.</param>
    /// <param name="body">A string containing the request body in JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> UpdatePlayerAsync(string? projectId, string? environmentId, string? serviceId, string? playerId, string lobbyId, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Update a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="playerId">The ID of the player to update.</param>
    /// <param name="lobbyId">The ID of the lobby to update.</param>
    /// <param name="body">A string containing the request body in JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<LobbyModel> UpdateLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, string body, CancellationToken cancellationToken);

    /// <summary>
    /// Heartbeat a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="lobbyId">The ID of the lobby to heartbeat.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task HeartbeatLobbyAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, CancellationToken cancellationToken);

    /// <summary>
    /// Request a token for a lobby.
    /// </summary>
    /// <param name="projectId">The project ID to use for the Lobby request.</param>
    /// <param name="environmentId">The environment ID to use for the Lobby request.</param>
    /// <param name="serviceId">The service ID to use for the Lobby request.</param>
    /// <param name="impersonatedPlayerId">The player ID to impersonate if applicable.</param>
    /// <param name="lobbyId">The ID of the lobby.</param>
    /// <param name="tokenType">The token type to request. Must be one of `vivoxJoin` or `wireJoin`.</param>
    /// <param name="cancellationToken">Token used to cancel the task.</param>
    public Task<IDictionary<string, TokenData>> RequestTokenAsync(string? projectId, string? environmentId, string? serviceId, string? impersonatedPlayerId, string lobbyId, TokenTypeEnum tokenType, CancellationToken cancellationToken);
}
