using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.EconomyTests;

public class EconomyPublishTests : UgsCliFixture
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

    #region list

    [Test]
    public async Task GetPublished_ThrowsWhenNotAuthenticated()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command("economy list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPublished_ThrowsWithProjectIdMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPublished_ThrowsWithProjectIdEmpty()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy list --project-id \"\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPublished_ThrowsWithEnvironmentNameMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await NewUgsCliTestCase()
            .Command($"economy list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingEnvironmentNameOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPublished_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"economy list")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task GetPublished_SucceedsWithValidInput_JsonFormattedOutput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"economy list -j")
            .AssertNoErrors()
            .ExecuteAsync();
    }
    #endregion

    #region publish
    [Test]
    public async Task Publish_ThrowsWhenNotAuthenticated()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command("economy publish")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Publish_ThrowsWithProjectIdMissing()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy publish")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Publish_ThrowsWithProjectIdEmpty()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command($"economy publish --project-id \"\"")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingProjectIdOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Publish_ThrowsWithEnvironmentNameMissing()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await NewUgsCliTestCase()
            .Command($"economy publish")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_MissingEnvironmentNameOutput)
            .ExecuteAsync();
    }

    [Test]
    public async Task Publish_SucceedsWithValidInput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"economy publish")
            .ExecuteAsync();
    }

    #endregion
}
