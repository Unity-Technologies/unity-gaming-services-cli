using Moq;
using NUnit.Framework;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Model;
using static Unity.Services.MpsLobby.LobbyApiV1.Generated.Model.TokenRequest;
using Unity.Services.Cli.Lobby.Service;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Newtonsoft.Json;
using System.Reflection;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Exceptions;
using IAuthApiAsync = Unity.Services.Gateway.Auth.Generated.Api.IDefaultApiAsync;
using Unity.Services.Gateway.Auth.Generated.Model;

namespace Unity.Services.Cli.Lobby.UnitTest.Mock;

[TestFixture]
class LobbyServiceTests
{
    const string k_TestProjectId = "19494529-fb4e-42a6-ab5f-4087d0c3032f";
    const string k_TestEnvironmentId = "7d2ae94f-297a-47e7-b8de-1ac2ba905d8f";
    const string k_ServiceId = "test-service-id";
    const string k_TestAccessToken = "test-token";
    const string k_DefaultLobbyId = "test-lobby-1";
    const string k_SecondaryLobbyId = "test-lobby-2";
    const string k_NewLobbyId = "new-test-lobby";
    const string k_FakeLobbyId = "fake-lobby";
    const string k_TestPlayerId = "test-player";
    const string k_InvalidJson = "invalid JSON";

    static ConfigValidationException configValidationException = new ConfigValidationException(string.Empty, string.Empty, string.Empty);
    static CliException cliException = new CliException(ExitCode.HandledError);
    static HttpRequestException httpRequestException = new HttpRequestException();

    LobbyService? m_LobbyService;
    readonly LobbyApiV1AsyncMock m_LobbyApiMock = new();
    ConfigurationValidator m_ValidatorObject = new();
    Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    Mock<IAuthApiAsync> m_AuthApiObject = new();

    static readonly IEnumerable<TestCaseData> k_ValidateRequestIdsTestCases = new[]
    {
        new TestCaseData(configValidationException, k_TestProjectId, string.Empty, k_TestEnvironmentId),
        new TestCaseData(configValidationException, string.Empty, k_TestEnvironmentId, k_TestEnvironmentId),
        new TestCaseData(cliException, k_TestProjectId, k_TestEnvironmentId, string.Empty),
    };

    static readonly IEnumerable<TestCaseData> k_MissingOrInvalidPlayerTestCases = new[]
    {
        new TestCaseData(cliException, null),
        new TestCaseData(httpRequestException, "invalid-player"),
    };

    static readonly IEnumerable<TestCaseData> k_RequestTokenInvalidInputTestCases = new[]
{
        new TestCaseData(cliException, null, k_DefaultLobbyId),
        new TestCaseData(httpRequestException, k_TestPlayerId, k_FakeLobbyId),
    };

    [SetUp]
    public void SetUp()
    {
        var types = new List<TypeInfo>
        {
            typeof(LobbyApiEndpoints).GetTypeInfo(),
            typeof(UnityServicesGatewayEndpoints).GetTypeInfo(),
        };
        EndpointHelper.InitializeNetworkTargetEndpoints(types);

        m_ValidatorObject = new ConfigurationValidator();
        m_AuthenticationServiceObject = new Mock<IServiceAccountAuthenticationService>();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_AuthApiObject = new Mock<IAuthApiAsync>();
        m_AuthApiObject.Setup(a => a.ExchangeToStatelessAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ExchangeRequest>(), It.IsAny<int>(), CancellationToken.None))
            .Returns(Task.FromResult(new ExchangeResponse("test-access-token")));

        var authConfig = new Mock<Gateway.Auth.Generated.Client.IReadableConfiguration>();
        authConfig.SetupGet(c => c.DefaultHeaders).Returns(new Dictionary<string, string>());
        m_AuthApiObject.SetupGet(a => a.Configuration).Returns(authConfig.Object);

        m_LobbyApiMock.SetUp();
        m_LobbyService = new LobbyService(m_ValidatorObject, m_AuthenticationServiceObject.Object, m_LobbyApiMock.DefaultLobbyClient.Object, m_AuthApiObject.Object);
    }

    [TestCaseSource(nameof(k_ValidateRequestIdsTestCases))]
    public void ValidateRequestIds_ThrowsForInvalidIds<TException>(TException _, string projectId, string environmentId, string serviceId) where TException : Exception
    {
        Assert.ThrowsAsync<TException>(async () => await m_LobbyService!.CreateLobbyAsync(projectId, environmentId, serviceId, null, string.Empty, default));
    }

    [Test]
    public async Task BulkUpdateLobby_SucceedsWithValidUpdate()
    {
        const string lobbyName = "bulk-updated-lobby-name";
        const string playerId = "new-player-by-bulk-update";
        var lobbyUpdate = new UpdateRequest(name: lobbyName);
        var player = new Player(id: playerId);
        var playersToAdd = new List<Player>() { player };
        var bulkUpdateRequest = new BulkUpdateRequest(lobbyUpdate: lobbyUpdate, playersToAdd: playersToAdd);

        var requestBody = JsonConvert.SerializeObject(bulkUpdateRequest);
        var updatedLobby = await m_LobbyService!.BulkUpdateLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_DefaultLobbyId, requestBody, default);
        Assert.AreEqual(lobbyName, updatedLobby.Name);
        Assert.Contains(player, updatedLobby.Players);
    }

    [Test]
    public void BulkUpdateLobby_ThrowsIfDeserializationFails()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.BulkUpdateLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_DefaultLobbyId, k_InvalidJson, default));
    }

    [Test]
    public async Task CreateLobbyAsync_CreatesLobbySuccessfully()
    {
        const int maxPlayers = 4;
        var createLobbyRequest = new CreateRequest(k_NewLobbyId, maxPlayers);
        var requestBody = JsonConvert.SerializeObject(createLobbyRequest);
        var lobby = await m_LobbyService!.CreateLobbyAsync(
                    k_TestProjectId,
                    k_TestEnvironmentId,
                    k_ServiceId,
                    null,
                    requestBody,
                    default);

        Assert.AreEqual(lobby.Name, k_NewLobbyId);
        Assert.AreEqual(lobby.MaxPlayers, maxPlayers);
        Assert.AreEqual(lobby.HostId, k_ServiceId);
    }

    [Test]
    public void CreateLobbyAsync_ThrowsIfDeserializationFails()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.CreateLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_InvalidJson, default));
    }

    [Test]
    public void DeleteLobbyAsync_DeletesLobbySuccessfully()
    {
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.DeleteLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_NewLobbyId, default));
    }

    [Test]
    public void DeleteLobbyAsync_ThrowsIfLobbyNotFound()
    {
        Assert.ThrowsAsync<HttpRequestException>(async () => await m_LobbyService!.DeleteLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_FakeLobbyId, default));
    }

    [Test]
    public async Task GetJoinedLobbies_GetsLobbiesSuccessfully()
    {
        var lobbies = (await m_LobbyService!.GetJoinedLobbiesAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_TestPlayerId, default)).ToList();
        Assert.AreEqual(1, lobbies.Count);
        Assert.AreEqual(k_SecondaryLobbyId, lobbies.First());
    }

    [Test]
    public void GetJoinedLobbies_ThrowsIfPlayerIdIsMissing()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.GetJoinedLobbiesAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, default));
    }

    [Test]
    public async Task GetHostedLobbies_GetsLobbiesSuccessfully()
    {
        var lobbies = (await m_LobbyService!.GetHostedLobbiesAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, default)).ToList();
        Assert.AreEqual(3, lobbies.Count);
        Assert.AreEqual(k_DefaultLobbyId, lobbies[0]);
        Assert.AreEqual(k_SecondaryLobbyId, lobbies[1]);
    }

    [Test]
    public void GetLobby_GetsLobbySuccessfully()
    {
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.GetLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, "test-lobby-3", default));
    }

    [Test]
    public void GetLobby_ThrowsIfLobbyNotFound()
    {
        Assert.ThrowsAsync<HttpRequestException>(async () => await m_LobbyService!.GetLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, "test-lobby-4", default));
    }

    [Test]
    public async Task JoinLobby_JoinsByIdSuccessfully()
    {
        const string playerId = "new-player-by-id";
        var player = new Player(id: playerId);
        var playerString = JsonConvert.SerializeObject(player);
        var lobby = await m_LobbyService!.JoinLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_DefaultLobbyId, null, playerString, default);

        Assert.IsNotEmpty(lobby.Players);
        Assert.Contains(player, lobby.Players);
    }

    [Test]
    public async Task JoinLobby_JoinsByCodeSuccessfully()
    {
        const string playerId = "new-player-by-code";
        var player = new Player(id: playerId);
        var playerString = JsonConvert.SerializeObject(player);
        var lobby = await m_LobbyService!.JoinLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, "code-1", playerString, default);

        Assert.IsNotEmpty(lobby.Players);
        Assert.Contains(player, lobby.Players);
    }
    [Test]
    public void JoinLobby_ThrowsIfIdAndCodeAreBothMissing()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.JoinLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, null, string.Empty, default));
    }

    [Test]
    public void Reconnect_SucceedsIfPlayerIsInLobby()
    {
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.ReconnectAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_TestPlayerId, k_SecondaryLobbyId, default));
    }

    [TestCaseSource(nameof(k_MissingOrInvalidPlayerTestCases))]
    public void Reconnect_ThrowsIfPlayerIsMissingOrInvalid<TException>(TException _, string? playerId) where TException : Exception
    {
        Assert.ThrowsAsync<TException>(async () => await m_LobbyService!.ReconnectAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, playerId, k_SecondaryLobbyId, default));
    }

    [Test]
    public void Query_SucceedsWithEmptyBody()
    {
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.QueryLobbiesAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, null, default));
    }

    [Test]
    public void Query_SucceedsWithSerializedBody()
    {
        var queryRequest = new QueryRequest(count: 10, sampleResults: true);
        var requestBody = JsonConvert.SerializeObject(queryRequest);
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.QueryLobbiesAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, requestBody, default));
    }

    [Test]
    public void Query_ThrowsIfDeserializationFails()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.QueryLobbiesAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_InvalidJson, default));
    }

    [Test]
    public void QuickJoin_SucceedsWithSerializedBody()
    {
        var filters = new List<QueryFilter>() { new QueryFilter(QueryFilter.FieldEnum.AvailableSlots, "1", QueryFilter.OpEnum.GE) };
        var filtersString = JsonConvert.SerializeObject(filters);
        var player = new Player(id: "quick-join-player");
        var playerString = JsonConvert.SerializeObject(player);
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.QuickJoinAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, filtersString, playerString, default));
    }

    [Test]
    public void QuickJoin_ThrowsIfDeserializationFails()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.QuickJoinAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_InvalidJson, k_InvalidJson, default));
    }

    [Test]
    public async Task RemovePlayer_SucceedsWithValidPlayerId()
    {
        const string playerId = "player-to-remove";
        var player = new Player(id: playerId);
        var playerString = JsonConvert.SerializeObject(player);
        var lobby = await m_LobbyService!.JoinLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_DefaultLobbyId, null, playerString, default);

        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.RemovePlayerAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, playerId, k_DefaultLobbyId, default));
    }

    [TestCaseSource(nameof(k_MissingOrInvalidPlayerTestCases))]
    public void RemovePlayer_ThrowsIfPlayerIsMissingOrInvalid<TException>(TException _, string? playerId) where TException : Exception
    {
        Assert.ThrowsAsync<TException>(async () => await m_LobbyService!.RemovePlayerAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, playerId, k_DefaultLobbyId, default));
    }

    [Test]
    public async Task UpdatePlayer_SucceedsWithValidUpdate()
    {
        const string playerId = "player-to-update";
        var player = new Player(id: playerId);
        var playerString = JsonConvert.SerializeObject(player);
        var lobby = await m_LobbyService!.JoinLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_DefaultLobbyId, null, playerString, default);

        const string allocationId = "new-allocation-id";
        var playerUpdate = new PlayerUpdateRequest(allocationId: allocationId);
        var requestBody = JsonConvert.SerializeObject(playerUpdate);
        var updatedLobby = await m_LobbyService!.UpdatePlayerAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, playerId, k_DefaultLobbyId, requestBody, default);
        var updatedPlayer = updatedLobby.Players.Where(p => p.Id.Equals(playerId)).First();
        Assert.AreEqual(allocationId, updatedPlayer.AllocationId);
    }

    [TestCaseSource(nameof(k_MissingOrInvalidPlayerTestCases))]
    public void UpdatePlayer_ThrowsIfPlayerIsMissingOrInvalid<TException>(TException _, string? playerId) where TException : Exception
    {
        Assert.ThrowsAsync<TException>(async () => await m_LobbyService!.UpdatePlayerAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, playerId, k_DefaultLobbyId, string.Empty, default));
    }

    [Test]
    public void UpdatePlayer_ThrowsIfDeserializationFails()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.UpdatePlayerAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, "player-id", k_DefaultLobbyId, k_InvalidJson, default));
    }

    [Test]
    public async Task UpdateLobby_SucceedsWithValidUpdate()
    {
        const string lobbyName = "updated-lobby-name";
        var lobbyUpdate = new UpdateRequest(name: lobbyName);
        var requestBody = JsonConvert.SerializeObject(lobbyUpdate);
        var updatedLobby = await m_LobbyService!.UpdateLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_DefaultLobbyId, requestBody, default);
        Assert.AreEqual(lobbyName, updatedLobby.Name);
    }

    [Test]
    public void UpdateLobby_ThrowsIfDeserializationFails()
    {
        Assert.ThrowsAsync<CliException>(async () => await m_LobbyService!.UpdateLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_DefaultLobbyId, k_InvalidJson, default));
    }

    [Test]
    public void HeartbeatLobby_SucceedsForExistingLobby()
    {
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.HeartbeatLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_DefaultLobbyId, default));
    }

    [Test]
    public void HeartbeatLobby_ThrowsIfLobbyNotFound()
    {
        Assert.ThrowsAsync<HttpRequestException>(async () => await m_LobbyService!.HeartbeatLobbyAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, null, k_FakeLobbyId, default));
    }

    [Test]
    public void RequestToken_SucceedsWithValidInput()
    {
        Assert.DoesNotThrowAsync(async () => await m_LobbyService!.RequestTokenAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, k_TestPlayerId, k_DefaultLobbyId, TokenTypeEnum.WireJoin, default));
    }

    [TestCaseSource(nameof(k_RequestTokenInvalidInputTestCases))]
    public void RequestToken_ThrowsWithInvalidInput<TException>(TException _, string? playerId, string lobbyId) where TException : Exception
    {
        Assert.ThrowsAsync<TException>(async () => await m_LobbyService!.RequestTokenAsync(k_TestProjectId, k_TestEnvironmentId, k_ServiceId, playerId, lobbyId, TokenTypeEnum.WireJoin, default));
    }
}
