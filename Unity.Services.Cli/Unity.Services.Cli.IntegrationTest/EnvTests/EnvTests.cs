using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;

namespace Unity.Services.Cli.IntegrationTest.EnvTests;

public class EnvTests : UgsCliFixture
{
    const string k_NotLoggedInMessage
        = "You are not logged into any service account. Please login using the 'ugs login' command.";

    readonly MockApi m_MockApi = new(NetworkTargetEndpoints.MockServer);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        m_MockApi.InitServer();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_MockApi.Server?.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        var environmentModels = await IdentityV1MockServerModels.GetModels();
        m_MockApi.Server?.WithMapping(environmentModels.ToArray());
    }

    // env list tests
    [Test]
    public async Task EnvironmentListThrowsProjectIdNotSetException()
    {
        var expectedMsg = "'project-id' is not set in project configuration." +
                          " 'UGS_CLI_PROJECT_ID' is not set in system environment variables";

        await GetLoggedInCli()
            .Command("env list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutput(output => Assert.IsTrue(output.Contains(expectedMsg)))
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentListThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await new UgsCliTestCase()
            .Command("env list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_NotLoggedInMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentListReturnsZeroExitCode()
    {
        var expectedReturn = $"\"{CommonKeys.ValidEnvironmentName}\": \"{CommonKeys.ValidEnvironmentId}\"";

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env list")
            .AssertStandardOutputContains(expectedReturn)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentListWithJsonOptionReturnsZeroExitCode()
    {
        var expectedReturn = string.Format(@"{{
  ""Result"": [
    {{
      ""id"": ""{0}"",
      ""projectId"": ""{1}"",
      ""name"": ""production"",
      ""isDefault"": true,", CommonKeys.ValidEnvironmentId, "390121ca-bb43-494f-b418-55be4e0c0faf");

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env list -j")
            .AssertStandardOutputContains(expectedReturn)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    // env add tests
    [Test]
    public async Task EnvironmentAdditionThrowsProjectIdNotSetException()
    {
        var expectedMsg = "'project-id' is not set in project configuration." +
                          " 'UGS_CLI_PROJECT_ID' is not set in system environment variables";

        await new UgsCliTestCase()
            .Command("env add 1")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentAdditionThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", "12345678-1111-2222-3333-123412341234");
        await new UgsCliTestCase()
            .Command($"env add test1")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_NotLoggedInMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentAdditionThrowsInvalidEnvironmentFormatException()
    {
        var expectedMsg = "Your environment-name is not valid. Valid input should have only alphanumerical and dash (-) characters.";

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env add test@")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentAdditionReturnsZeroExitCode()
    {
        var expectedMsg = "'test-env-123' added";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("env add test-env-123")
            .AssertStandardOutputContains(expectedMsg)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    // env delete tests
    [Test]
    public async Task EnvironmentDeleteThrowsProjectIdNotSetException()
    {
        var expectedMsg = "'project-id' is not set in project configuration." +
                          " 'UGS_CLI_PROJECT_ID' is not set in system environment variables";

        await new UgsCliTestCase()
            .Command("env delete test-env-123")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentDeleteThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await new UgsCliTestCase()
            .Command("env delete test-env-123")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(k_NotLoggedInMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentDeleteThrowsInvalidEnvironmentFormatException()
    {
        var expectedMsg = "Your environment-name is not valid. Valid input should have only alphanumerical and dash (-) characters.";

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env delete test@")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentDeleteReturnsZeroExitCode()
    {
        var expectedMsg = $"Deleted environment '{CommonKeys.ValidEnvironmentName}'";

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command($"env delete \"{CommonKeys.ValidEnvironmentName}\"")
            .AssertStandardOutputContains(expectedMsg)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    // env use tests
    [Test]
    public async Task EnvironmentUseThrowsInvalidEnvironmentFormatException()
    {
        var expectedMsg = "Your environment-name is not valid. Valid input should have only alphanumerical and dash (-) characters.";

        await new UgsCliTestCase()
            .Command("env use test@")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentUseReturnsZeroExitCode()
    {
        await new UgsCliTestCase()
            .Command("env use 123")
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
