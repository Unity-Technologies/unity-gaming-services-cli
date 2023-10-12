using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer;
using Unity.Services.Gateway.PlayerAdminApiV3.Generated.Model;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.Player.Model;

namespace Unity.Services.Cli.IntegrationTest.PlayerTests;

public class PlayerTests : UgsCliFixture
{

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        MockApi.Server?.AllowPartialMapping();
    }

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        await MockApi.MockServiceAsync(new PlayerApiMock());
    }

    [Test]
    public async Task CreatePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command("player create")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CreatePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command("player create")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CreatePlayer_Succeeds()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("player create")
            .ExecuteAsync();
    }

    [Test]
    public async Task DeletePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player delete {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeletePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player delete {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeletePlayer_Throws_NoPlayedId()
    {
        var expectedMsg = "Required argument missing for command: 'delete'.";

        await GetLoggedInCli()
            .Command("player delete")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeletePlayer_Succeeds()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"player delete {PlayerApiMock.PlayerId}")
            .ExecuteAsync();
    }

    [Test]
    public async Task DisablePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player disable {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DisablePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player disable {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DisablePlayer_Throws_NoPlayedId()
    {
        var expectedMsg = "Required argument missing for command: 'disable'.";

        await GetLoggedInCli()
            .Command("player disable")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DisablePlayer_Succeeds()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"player disable {PlayerApiMock.PlayerId}")
            .ExecuteAsync();
    }

    [Test]
    public async Task EnablePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player enable {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnablePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player enable {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnablePlayer_Throws_NoPlayedId()
    {
        var expectedMsg = "Required argument missing for command: 'enable'.";

        await GetLoggedInCli()
            .Command("player enable")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnablePlayer_Succeeds()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"player disable {PlayerApiMock.PlayerId}")
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player get {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player get {PlayerApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPlayer_Throws_NoPlayedId()
    {
        var expectedMsg = "Required argument missing for command: 'get'.";

        await GetLoggedInCli()
            .Command("player get")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPlayer_Succeeds()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"player get {PlayerApiMock.PlayerId}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPlayer_SucceedsReturnsJson()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"player get {PlayerApiMock.PlayerId} -j")
            .AssertStandardOutputContains(JsonConvert.SerializeObject(new PlayerAuthPlayerProjectResponse(), Formatting.Indented))
            .ExecuteAsync();
    }

    [Test]
    public async Task ListPlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task ListPlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task ListPlayer_Succeeds()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        var result = new PlayerListResponseResult(PlayerApiMock.GetPlayerListMock());

        await GetLoggedInCli()
            .Command($"player list")
            .AssertNoErrors()
            .AssertStandardOutputContains(result.ToString())
            .ExecuteAsync();
    }

    [Test]
    public async Task ListPlayer_SucceedsReturnsJson()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command($"player list -j")
            .AssertStandardOutputContains(JsonConvert.SerializeObject(new PlayerListResponseResult(PlayerApiMock.GetPlayerListMock()), Formatting.Indented))
            .ExecuteAsync();
    }
}
