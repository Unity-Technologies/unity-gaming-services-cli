using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.IntegrationTest.EnvTests;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Model;

namespace Unity.Services.Cli.IntegrationTest.LobbyTests;

public class LobbyTests : UgsCliFixture
{
    const string k_ServiceId = "lobby-cli-test-service";
    const string k_ServiceIdInput = $"--service-id {k_ServiceId}";
    const string k_DefaultOptions = $"{k_ServiceIdInput} --json";
    const string k_InvalidJson = "invalidjson";
    const string k_LobbyId = "test-lobby-id";
    const string k_PlayerId = "test-player-id";
    const string k_ConfigId = "test-config-id";
    const string k_RequiredArgumentMissing = "Required argument missing for command:";
    const string k_FailedToDeserialize = "Failed to deserialize object for Lobby request.";

    readonly MockApi m_MockApi = new(NetworkTargetEndpoints.MockServer);
    const string k_AuthV1OpenApiUrl = "https://services.docs.unity.com/specs/v1/61757468.yaml";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        m_MockApi.InitServer();
        m_MockApi.Server?.AllowPartialMapping();

        var environmentModels = await IdentityV1MockServerModels.GetModels();
        m_MockApi.Server?.WithMapping(environmentModels.ToArray());

        var authServiceModels = await MappingModelUtils.ParseMappingModelsAsync(k_AuthV1OpenApiUrl, new());
        m_MockApi.Server?.WithMapping(authServiceModels.ToArray());

        var lobbyServiceModels = await MappingModelUtils.ParseMappingModelsFromGeneratorConfigAsync("lobby-api-v1-generator-config.yaml", new());
        m_MockApi.Server?.WithMapping(lobbyServiceModels.ToArray());
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_MockApi.Server?.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
    }

    [Test]
    public async Task CommandThrowsIfServiceIdIsMissing()
    {
        var expectedMsg = "Option '--service-id' is required.";
        await AuthenticatedCommand()
            .Command("lobby query")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CommandThrowsIfServiceIdIsEmpty()
    {
        var expectedMsg = "A non-empty service ID must be provided.";
        await AuthenticatedCommand()
            .Command("lobby query --service-id \"\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CommandThrowsIfProjectIdIsMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);

        var expectedMsg = "'project-id' is not set in project configuration";
        await new UgsCliTestCase()
            .Command($"lobby get {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CommandThrowsIfEnvironmentNameIsMissing()
    {
        var expectedMsg = "'environment-name' is not set in project configuration";
        await new UgsCliTestCase()
            .Command($"lobby get {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task BulkUpdate_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby bulk-update {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task BulkUpdate_ThrowsIfRequestBodyMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby bulk-update {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task BulkUpdate_ThrowsIfRequestBodyIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby bulk-update {k_LobbyId} {k_InvalidJson} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_FailedToDeserialize)
            .ExecuteAsync();
    }

    [Test]
    public async Task BulkUpdate_SucceedsWithValidInput()
    {
        var lobbyUpdate = new UpdateRequest(name: "new-lobby-name");
        var bulkUpdate = new BulkUpdateRequest(lobbyUpdate: lobbyUpdate);
        var bulkUpdateString = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(bulkUpdate));

        await AuthenticatedCommand()
            .Command($"lobby bulk-update {k_LobbyId} {bulkUpdateString} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Create_ThrowsIfRequestBodyIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby create {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task Create_SucceedsWithValidInput()
    {
        var createRequest = new CreateRequest(name: "test-lobby", maxPlayers: 4);
        var requestBody = JsonConvert.SerializeObject(createRequest).Replace("\"", "\\\"");

        await AuthenticatedCommand()
            .Command($"lobby create {requestBody} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Create_ThrowsIfRequestBodyIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby create {k_InvalidJson} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_FailedToDeserialize)
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby delete {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_SucceedsWithValidInput()
    {
        await AuthenticatedCommand()
            .Command($"lobby delete {k_LobbyId} {k_DefaultOptions}")
            .AssertStandardOutputContains("Lobby successfully deleted")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task GetHosted_SucceedsWithoutPlayerId()
    {
        await AuthenticatedCommand()
            .Command($"lobby get-hosted {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task GetHosted_SucceedsWithPlayerId()
    {
        await AuthenticatedCommand()
            .Command($"lobby get-hosted --player-id {k_PlayerId} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task GetJoined_ThrowsIfPlayerIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby get-joined {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetJoined_SucceedsWithValidInput()
    {
        await AuthenticatedCommand()
            .Command($"lobby get-joined {k_PlayerId} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby get {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_SucceedsWithValidInput()
    {
        await AuthenticatedCommand()
            .Command($"lobby get {k_LobbyId} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Join_ThrowsIfPlayerDetailsAreMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby join {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task Join_ThrowsIfIdAndCodeAreBothMissing()
    {
        var playerString = GetPlayerDetailsString(k_PlayerId);

        await AuthenticatedCommand()
            .Command($"lobby join {playerString} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains("Either lobby ID or lobby code must be provided to join a lobby.")
            .ExecuteAsync();
    }

    [Test]
    public async Task Join_ThrowsIfRequestBodyIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby join {k_InvalidJson} {k_DefaultOptions} --lobby-id {k_LobbyId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_FailedToDeserialize)
            .ExecuteAsync();
    }

    [Test]
    public async Task Join_SucceedsWithLobbyId()
    {
        var playerString = GetPlayerDetailsString(k_PlayerId);

        await AuthenticatedCommand()
            .Command($"lobby join {playerString} {k_DefaultOptions} --lobby-id {k_LobbyId}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Join_SucceedsWithLobbyCode()
    {
        var playerString = GetPlayerDetailsString(k_PlayerId);

        await AuthenticatedCommand()
            .Command($"lobby join {playerString} {k_DefaultOptions} --lobby-code test-lobby-code")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Reconnect_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby reconnect {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task Reconnect_ThrowsIfPlayerIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby reconnect {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task Reconnect_SucceedsWithValidInput()
    {
        await AuthenticatedCommand()
            .Command($"lobby reconnect {k_LobbyId} {k_PlayerId} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task QuickJoin_ThrowsIfFilterIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby quickjoin {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task QuickJoin_ThrowsIfPlayerDetailsAreMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby quickjoin \"\" {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task QuickJoin_ThrowsIfRequestBodyIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby quickjoin {k_InvalidJson} {k_InvalidJson} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_FailedToDeserialize)
            .ExecuteAsync();
    }

    [Test]
    public async Task QuickJoin_SucceedsWithValidInput()
    {
        var filters = new List<QueryFilter>() { new QueryFilter(QueryFilter.FieldEnum.AvailableSlots, "1", QueryFilter.OpEnum.GE) };
        var filtersString = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(filters));
        var playerString = GetPlayerDetailsString(k_PlayerId);

        await AuthenticatedCommand()
            .Command($"lobby quickjoin {filtersString} {playerString} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task RemovePlayer_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby player remove {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task RemovePlayer_ThrowsIfPlayerIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby player remove {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task RemovePlayer_SucceedsWithValidInput()
    {
        await AuthenticatedCommand()
            .Command($"lobby player remove {k_LobbyId} {k_PlayerId} {k_DefaultOptions}")
            .AssertStandardOutputContains("Player successfully removed")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdatePlayer_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby player update {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdatePlayer_ThrowsIfPlayerIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby player update {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdatePlayer_ThrowsIfRequestBodyMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby player update {k_LobbyId} {k_PlayerId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdatePlayer_ThrowsIfRequestBodyIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby player update {k_LobbyId} {k_PlayerId} {k_InvalidJson} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_FailedToDeserialize)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdatePlayer_SucceedsWithValidInput()
    {
        var playerUpdate = new PlayerUpdateRequest(connectionInfo: "new-connection-info");
        var playerUpdateString = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(playerUpdate));

        await AuthenticatedCommand()
            .Command($"lobby player update {k_LobbyId} {k_PlayerId} {playerUpdateString} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdateLobby_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby update {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdateLobby_ThrowsIfRequestBodyMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby update {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdateLobby_ThrowsIfRequestBodyIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby update {k_LobbyId} {k_InvalidJson} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_FailedToDeserialize)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdateLobby_SucceedsWithValidInput()
    {
        var lobbyUpdate = new UpdateRequest(name: "new-lobby-name");
        var lobbyUpdateString = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(lobbyUpdate));

        await AuthenticatedCommand()
            .Command($"lobby update {k_LobbyId} {lobbyUpdateString} {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task HeartbeatLobby_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby heartbeat {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task HeartbeatLobby_SucceedsWithValidInput()
    {
        await AuthenticatedCommand()
            .Command($"lobby heartbeat {k_LobbyId} {k_DefaultOptions}")
            .AssertStandardOutputContains("Lobby successfully heartbeated")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task RequestToken_ThrowsIfLobbyIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby request-token {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task RequestToken_ThrowsIfPlayerIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby request-token {k_LobbyId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task RequestToken_ThrowsIfTokenTypeIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby request-token {k_LobbyId} {k_PlayerId} {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task RequestToken_ThrowsIfTokenTypeIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby request-token {k_LobbyId} {k_PlayerId} InvalidTokenType {k_DefaultOptions}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("Cannot parse argument")
            .ExecuteAsync();
    }

    [Test]
    public async Task RequestToken_SucceedsWithValidInput()
    {
        await AuthenticatedCommand()
            .Command($"lobby request-token {k_LobbyId} {k_PlayerId} WireJoin {k_DefaultOptions}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdateConfig_ThrowsIfConfigIdIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby config update")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdateConfig_ThrowsIfRequestBodyIsMissing()
    {
        await AuthenticatedCommand()
            .Command($"lobby config update {k_ConfigId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task UpdateConfig_ThrowsIfRequestBodyIsInvalid()
    {
        await AuthenticatedCommand()
            .Command($"lobby config update {k_ConfigId} ''")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains("Failed to deserialize config update request body.")
            .ExecuteAsync();
    }

    [Test]
    [Ignore("TODO: Add Remote Config API spec and mock server models.")]
    public async Task UpdateConfig_SucceedsWithValidInput()
    {
        var updateRequest = new UpdateConfigRequest
        {
            Type = "lobby",
            Value = new List<object>(),
        };
        var updateRequestString = HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(updateRequest));

        await AuthenticatedCommand()
            .Command($"lobby config update {k_ConfigId} {updateRequestString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    UgsCliTestCase AuthenticatedCommand()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        SetConfigValue("environment-id", CommonKeys.ValidEnvironmentId);

        return GetLoggedInCli();
    }

    static string GetPlayerDetailsString(string playerId)
    {
        var player = new Player(id: playerId);
        return HttpUtility.JavaScriptStringEncode(JsonConvert.SerializeObject(player));
    }
}
