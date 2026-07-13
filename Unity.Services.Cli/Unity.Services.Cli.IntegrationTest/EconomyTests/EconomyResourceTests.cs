using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.EconomyTests;

public class EconomyResourceTests : UgsCliFixture
{
    const string k_NotLoggedInOutput =
        " You are not logged into any service account. Please login using the 'ugs login' command.";

    const string k_MissingProjectIdOutput = "'project-id' is not set in project configuration";
    const string k_MissingEnvironmentNameOutput = "'environment-name' is not set in project configuration";

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        await MockApi.MockServiceAsync(new EconomyApiMock());
        await MockApi.MockServiceAsync(new IdentityV1Mock());
    }

    [TearDown]
    public void TearDown()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        MockApi.Server?.ResetMappings();
    }

    #region list --include-draft

    [Test]
    public async Task Get_ThrowsWhenNotAuthenticated()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command("economy list --include-draft")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_ThrowsWithProjectIdMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy list --include-draft")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_ThrowsWithProjectIdEmpty()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy list --include-draft --project-id \"\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_ThrowsWithEnvironmentNameMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await NewUgsCliTestCase()
            .Command($"economy list --include-draft")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingEnvironmentNameOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Get_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"economy list --include-draft")
            .AssertNoErrors()
            .ExecuteAsync();
    }
    #endregion

    #region delete resource
    [Test]
    public async Task Delete_ThrowsWhenNotAuthenticated()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command("economy delete SWORD")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_ThrowsWithProjectIdMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy delete SWORD")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_ThrowsWithProjectIdEmpty()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy delete SWORD --project-id \"\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_ThrowsWithEnvironmentNameMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await NewUgsCliTestCase()
            .Command($"economy delete SWORD")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingEnvironmentNameOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Delete_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"economy delete SWORD")
            .ExecuteAsync();
    }
    #endregion
}
