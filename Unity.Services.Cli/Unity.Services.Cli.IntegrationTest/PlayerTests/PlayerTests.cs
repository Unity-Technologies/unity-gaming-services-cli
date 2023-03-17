using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;

namespace Unity.Services.Cli.IntegrationTest.PlayerTests;

public class PlayerTests : UgsCliFixture
{
    private const string k_AdminApiUrl = "https://services.docs.unity.com/specs/v1/706c617965722d617574682d61646d696e.yaml";
    const string k_PlayerAuthApiUrl = "https://services.docs.unity.com/specs/v1/706c617965722d61757468.yaml";

    private const string k_PlayerId = "player-id";
    MockApi m_MockApi = new(NetworkTargetEndpoints.MockServer);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        m_MockApi.InitServer();
        m_MockApi.Server?.AllowPartialMapping();
    }

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        var playerAuthenticationAdminServiceModels = await MappingModelUtils.ParseMappingModelsAsync(k_AdminApiUrl, new ());
        playerAuthenticationAdminServiceModels = playerAuthenticationAdminServiceModels.Select(m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId));
        m_MockApi.Server?.WithMapping(playerAuthenticationAdminServiceModels.ToArray());

        var playerAuthenticationServiceModels = await MappingModelUtils.ParseMappingModelsAsync(k_PlayerAuthApiUrl, new ());
        playerAuthenticationServiceModels = playerAuthenticationServiceModels.Select(m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId));
        m_MockApi.Server?.WithMapping(playerAuthenticationServiceModels.ToArray());
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_MockApi.Server?.Dispose();
    }

    [Test]
    public async Task CreatePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command("player create")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CreatePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command("player create")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task CreatePlayer_Succeeds()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("player create")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeletePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player delete {k_PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeletePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player delete {k_PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
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
            .Command($"player delete {k_PlayerId}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DisablePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player disable {k_PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task DisablePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player disable {k_PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
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
            .Command($"player disable {k_PlayerId}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task EnablePlayer_Throws_NotAuthenticated()
    {
        var expectedMsg = "You are not logged into any service account. Please login using the 'ugs login' command.";

        await new UgsCliTestCase()
            .Command($"player enable {k_PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnablePlayer_Throws_NoProjectId()
    {
        var expectedMsg = "Your project-id is not valid. The value cannot be null or empty.";

        await GetLoggedInCli()
            .Command($"player enable {k_PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
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
            .Command($"player disable {k_PlayerId}")
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
