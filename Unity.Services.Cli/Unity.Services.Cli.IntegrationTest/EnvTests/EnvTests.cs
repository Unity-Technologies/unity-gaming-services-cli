using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.EnvTests;

public class EnvTests : UgsCliFixture
{
    const string k_InvalidEnvNameMsg = ConfigurationValidator.EnvironmentNameInvalidMessage;

    const string k_NotLoggedInMessage
        = "You are not logged into any service account. Please login using the 'ugs login' command.";

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        await MockApi.MockServiceAsync(new IdentityV1Mock());
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
            .AssertStandardError(output => Assert.IsTrue(output.Contains(expectedMsg)))
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentListThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await new UgsCliTestCase()
            .Command("env list")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentListReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env list")
            .AssertStandardOutput(
                output =>
                {
                    StringAssert.Contains(CommonKeys.ValidEnvironmentName, output);
                    StringAssert.Contains(CommonKeys.ValidEnvironmentId, output);
                })
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentListWithJsonOptionReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env list -j")
            .AssertStandardOutput(output =>
            {
                Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(output));
                StringAssert.Contains(CommonKeys.ValidEnvironmentId, output);
            })
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
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentAdditionThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", "12345678-1111-2222-3333-123412341234");
        await new UgsCliTestCase()
            .Command($"env add test1")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentAdditionThrowsInvalidEnvironmentFormatException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env add test@")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_InvalidEnvNameMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentAdditionReturnsZeroExitCode()
    {
        var expectedMsg = "'test-env-123' added";
        SetConfigValue("project-id", CommonKeys.ValidProjectId);

        await GetLoggedInCli()
            .Command("env add test-env-123")
            .AssertStandardErrorContains(expectedMsg)
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
            .AssertStandardErrorContains(expectedMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentDeleteThrowsNotLoggedInException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await new UgsCliTestCase()
            .Command("env delete test-env-123")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_NotLoggedInMessage)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentDeleteThrowsInvalidEnvironmentFormatException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command("env delete test@")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_InvalidEnvNameMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentDeleteReturnsZeroExitCode()
    {
        var expectedMsg = $"Deleted environment '{CommonKeys.ValidEnvironmentName}'";

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command($"env delete \"{CommonKeys.ValidEnvironmentName}\"")
            .AssertStandardErrorContains(expectedMsg)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    // env use tests
    [Test]
    public async Task EnvironmentUseThrowsInvalidEnvironmentFormatException()
    {
        await new UgsCliTestCase()
            .Command("env use test@")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_InvalidEnvNameMsg)
            .ExecuteAsync();
    }

    [Test]
    public async Task EnvironmentUseReturnsZeroExitCode()
    {
        await new UgsCliTestCase()
            .Command("env use 123")
            .ExecuteAsync();
    }
}
