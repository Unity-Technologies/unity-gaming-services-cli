using Moq;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Model;
using LobbyModel = Unity.Services.MpsLobby.LobbyApiV1.Generated.Model.Lobby;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Api;

namespace Unity.Services.Cli.Lobby.UnitTest.Mock;

class LobbyApiV1AsyncMock
{
    public Mock<ILobbyApiAsync> DefaultLobbyClient = new();

    List<LobbyModel> m_TestLobbies = new List<LobbyModel>()
    {
        new LobbyModel(id: "test-lobby-1", lobbyCode: "code-1", maxPlayers: 8, hostId: "test-service-id", players: new List<Player>()),
        new LobbyModel(id: "test-lobby-2", lobbyCode: "code-2", maxPlayers: 8, hostId: "test-service-id", players: new List<Player>(){new Player(id: "test-player")}),
        new LobbyModel(id: "test-lobby-3", lobbyCode: "code-3", maxPlayers: 8, hostId: "test-service-id-2", players: new List<Player>()),
    };

    /// <summary>
    /// Sets up an extremely lightweight implementation of Lobby service logic in order to mock each API's response.
    /// </summary>
    public void SetUp()
    {
        DefaultLobbyClient = new Mock<ILobbyApiAsync>();

        DefaultLobbyClient.Setup(a =>
                a.BulkUpdateLobbyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<BulkUpdateRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string _, string _, BulkUpdateRequest bulkUpdateRequest, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                // For simplicity's sake, only mock name updates and player adds.
                lobby.Name = bulkUpdateRequest.LobbyUpdate.Name;
                lobby.Players.AddRange(bulkUpdateRequest.PlayersToAdd);
                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.CreateLobbyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CreateRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string serviceId, string _, CreateRequest createRequest, int _, CancellationToken _) =>
            {
                var lobby = new LobbyModel(
                    id: createRequest.Name,
                    name: createRequest.Name,
                    maxPlayers: createRequest.MaxPlayers,
                    hostId: serviceId,
                    players: new List<Player>());
                m_TestLobbies.Add(lobby);
                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.DeleteLobbyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string _, string _, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                return Task.CompletedTask;
            });

        DefaultLobbyClient.Setup(a =>
                a.GetJoinedLobbiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string _, string impersonatedUserId, int _, CancellationToken _) =>
            {
                var lobbies = new List<string>();
                foreach (var lobby in m_TestLobbies)
                {
                    foreach (var player in lobby.Players)
                    {
                        if (player.Id.Equals(impersonatedUserId))
                        {
                            lobbies.Add(lobby.Id);
                            break;
                        }
                    }
                }

                return Task.FromResult(lobbies);
            });

        DefaultLobbyClient.Setup(a =>
                a.GetHostedLobbiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string serviceId, string _, int _, CancellationToken _) =>
            {
                return Task.FromResult(m_TestLobbies.Where(l => l.HostId.Equals(serviceId)).Select(l => l.Id).ToList());
            });

        DefaultLobbyClient.Setup(a =>
                a.GetLobbyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string _, string _, string _, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.JoinLobbyByIdAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<JoinLobbyByIdRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string _, string _, JoinLobbyByIdRequest joinLobbyByIdRequest, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                lobby.Players.Add(joinLobbyByIdRequest.GetPlayer());
                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.JoinLobbyByCodeAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<JoinByCodeRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string _, string _, JoinByCodeRequest joinByCodeRequest, int _, CancellationToken _) =>
            {
                var lobby = m_TestLobbies.Where(l => l.LobbyCode.Equals(joinByCodeRequest.LobbyCode)).FirstOrDefault();
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                lobby.Players.Add(joinByCodeRequest.Player);
                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.ReconnectAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string _, string impersonatedUserId, object _, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                var player = GetPlayerInLobby(lobby, impersonatedUserId);
                if (player is null)
                {
                    throw new HttpRequestException();
                }

                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.QueryLobbiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns(Task.FromResult(new QueryResponse()));

        DefaultLobbyClient.Setup(a =>
                a.QuickJoinLobbyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<QuickJoinRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns(Task.FromResult(new LobbyModel()));

        DefaultLobbyClient.Setup(a =>
                a.RemovePlayerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string playerId, string _, string _, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                var player = GetPlayerInLobby(lobby, playerId);
                if (player is null)
                {
                    throw new HttpRequestException();
                }

                lobby.Players.Remove(player);
                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.UpdatePlayerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PlayerUpdateRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string playerId, string _, string _, PlayerUpdateRequest playerUpdateRequest, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                for (int i = 0; i < lobby.Players.Count; i++)
                {
                    if (lobby.Players[i].Id.Equals(playerId))
                    {
                        // For simplicity's sake, only mock allocation ID updates.
                        lobby.Players[i].AllocationId = playerUpdateRequest.AllocationId;
                        return Task.FromResult(lobby);
                    }
                }

                throw new HttpRequestException();
            });

        DefaultLobbyClient.Setup(a =>
                a.UpdateLobbyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<UpdateRequest>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string _, string _, UpdateRequest updateRequest, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                // For simplicity's sake, only mock name updates.
                lobby.Name = updateRequest.Name;
                return Task.FromResult(lobby);
            });

        DefaultLobbyClient.Setup(a =>
                a.HeartbeatAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, string _, string _, object _, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                return lobby is not null ? Task.CompletedTask : throw new HttpRequestException();
            });

        DefaultLobbyClient.Setup(a =>
                a.RequestTokensAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<TokenRequest>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0,
                    CancellationToken.None
                ))
            .Returns((string _, string _, string lobbyId, List<TokenRequest> _, string _, string _, int _, CancellationToken _) =>
            {
                var lobby = GetTestLobbyById(lobbyId);
                if (lobby is null)
                {
                    throw new HttpRequestException();
                }

                return Task.FromResult(new Dictionary<string, TokenData>());
            });
    }

    LobbyModel? GetTestLobbyById(string id)
    {
        return m_TestLobbies.Where(l => l.Id.Equals(id)).FirstOrDefault();
    }

    static Player? GetPlayerInLobby(LobbyModel lobby, string playerId)
    {
        var matchingPlayers = lobby.Players.Where(p => p.Id.Equals(playerId));
        return matchingPlayers.Any() ? matchingPlayers.First() : null;
    }
}
