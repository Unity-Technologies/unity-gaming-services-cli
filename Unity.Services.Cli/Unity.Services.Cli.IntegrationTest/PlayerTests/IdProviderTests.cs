using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.PlayerTests;

public class IdProviderTests : UgsCliFixture
{
    static readonly Dictionary<string, string?> k_Preview = new()
    {
        [Keys.EnvironmentKeys.Preview] = PreviewFeatures.PlayerIdProviders,
    };

    [Test]
    public async Task IdProviderCommands_AreAbsentWithoutThePreview()
    {
        await NewUgsCliTestCase()
            .WithEnvironmentVariables(new Dictionary<string, string?> { [Keys.EnvironmentKeys.Preview] = null })
            .Command("player id-provider list-types -j")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

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
        await MockApi.MockServiceAsync(new IdProviderApiMock());
    }

    [Test]
    public async Task ListTypes_NeedsNoLogin()
    {
        await NewUgsCliTestCase().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider list-types -j")
            .AssertNoErrors()
            .AssertStandardOutputContains("\"steampowered.com\"")
            .ExecuteAsync();
    }

    [Test]
    public async Task List_ReportsConfigIdsAndNoSecretField()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider list -j")
            .AssertNoErrors()
            .AssertStandardOutput(output =>
            {
                Assert.That(output, Does.Contain($"\"configId\": \"{IdProviderApiMock.SteamConfigId}\""));
                Assert.That(output, Does.Contain("\"usernamepassword\""));
                Assert.That(output, Does.Not.Contain("clientSecret"));
            })
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_ResolvesAnAliasToItsConfiguration()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider get Steam -j")
            .AssertNoErrors()
            .AssertStandardOutputContains($"\"configId\": \"{IdProviderApiMock.SteamConfigId}\"")
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_FetchesAConfigIdDirectly()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command($"player id-provider get {IdProviderApiMock.SteamConfigId} -j")
            .AssertNoErrors()
            .AssertStandardOutput(output =>
            {
                Assert.That(output, Does.Contain("\"name\": \"Steam\""));
                Assert.That(output, Does.Contain("\"hasClientSecret\": true"));
            })
            .ExecuteAsync();
    }

    [Test]
    public async Task Create_AddsTheProvider()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider create google --client-id client -j")
            .AssertNoErrors()
            .AssertStandardOutputContains($"\"configId\": \"{IdProviderApiMock.CreatedConfigId}\"")
            .ExecuteAsync();
    }

    [Test]
    public async Task Update_NamesTheFieldsItWrote()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider update steam --client-id 999 -j")
            .AssertNoErrors()
            .AssertStandardOutputContains($"Wrote client-id on configuration {IdProviderApiMock.SteamConfigId}")
            .ExecuteAsync();
    }

    [Test]
    public async Task Disable_ReportsTheConfigurationDisabled()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider disable steam -j")
            .AssertNoErrors()
            .AssertStandardOutputContains("\"enabled\": false")
            .ExecuteAsync();
    }

    [Test]
    public async Task Enable_ReportsTheConfigurationEnabled()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider enable steam -j")
            .AssertNoErrors()
            .AssertStandardOutputContains("\"enabled\": true")
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_RefusesWithoutForce()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider delete steam")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains("--force")
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_WithForceDeletes()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli().WithEnvironmentVariables(k_Preview)
            .Command("player id-provider delete steam --force -j")
            .AssertNoErrors()
            .AssertStandardOutputContains($"Deleted configuration {IdProviderApiMock.SteamConfigId}")
            .ExecuteAsync();
    }
}
