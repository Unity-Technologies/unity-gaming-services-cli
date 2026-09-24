using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.IntegrationTest.AccessTests;

public class AccessTests : UgsCliFixture
{
    const string k_ProjectIdNotSetErrorMessage = "'project-id' is not set in project configuration."
                                                 + " '" + Keys.EnvironmentKeys.ProjectId + "' is not set in system environment variables.";
    const string k_LoggedOutErrorMessage = "You are not logged into any service account."
                                           + " Please login using the 'ugs login' command.";
    const string k_EnvironmentNameNotSetErrorMessage = "'environment-name' is not set in project configuration."
                                                       + " '" + Keys.EnvironmentKeys.EnvironmentName + "' is not set in system environment variables.";

    readonly string m_TestDirectory = Path.GetFullPath(Path.Combine(UgsCliBuilder.RootDirectory, "Unity.Services.Cli/Unity.Services.Cli.IntegrationTest/AccessTests/Data/"));

    const string k_RequiredArgumentMissing = "Required argument missing for command";
    const string k_StatementId = "statement-1";

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        MockApi.Server?.ResetMappings();

        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new AccessApiMock());

    }

    [TearDown]
    public void TearDown()
    {
        MockApi.Server?.ResetMappings();
    }


    [Test, TestCaseSource(nameof(AccessModuleCommands))]
    public async Task AccessCommandsThrowsProjectIdNotSetException(string command)
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertException(command, k_ProjectIdNotSetErrorMessage);
    }

    [Test, TestCaseSource(nameof(AccessModuleCommands))]
    public async Task AccessCommandsThrowsNotLoggedInException(string command)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await NewUgsCliTestCase()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_LoggedOutErrorMessage)
            .ExecuteAsync();
    }

    [Test, TestCaseSource(nameof(AccessModuleCommands))]
    public async Task AccessCommandsThrowsEnvironmentIdNotSetException(string command)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await AssertException(command, k_EnvironmentNameNotSetErrorMessage);
    }

    [TestCase("access player-policy update")]
    [TestCase("access player-policy delete")]
    public async Task AccessCommandsThrowsPlayerIdNotSetException(string command)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    // access project-policy list
    [Test]
    public async Task AccessProjectPolicyListReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess("access project-policy list", expectedStdOut: "statement-1");
    }

    // access player-policy list --player-id
    [Test]
    public async Task AccessPlayerPolicyListWithPlayerIdReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        var playerPolicy = new
        {
            playerId = AccessApiMock.PlayerId,
            statements = new List<PlayerStatement>()
        };

        await AssertSuccess($"access player-policy list --player-id {AccessApiMock.PlayerId}", expectedStdOut: JsonConvert.SerializeObject(playerPolicy, Formatting.Indented));
    }

    // access player-policy list (all)
    [Test]
    public async Task AccessPlayerPolicyListAllReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        var playerPolicy = new
        {
            playerId = AccessApiMock.PlayerId,
            statements = new List<PlayerStatement>()
        };

        List<object> obj = new List<object>();
        obj.Add(playerPolicy);

        await AssertSuccess("access player-policy list", expectedStdOut: JsonConvert.SerializeObject(obj, Formatting.Indented));
    }

    // access player-policy update
    [Test]
    public async Task AccessPlayerPolicyUpdateReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess($"access player-policy update {AccessApiMock.PlayerId} {Path.Combine(m_TestDirectory, "policy.json")}", $"Policy for player: '{AccessApiMock.PlayerId}' has been updated");
    }

    // access project-policy delete
    [Test]
    public async Task AccessProjectPolicyDeleteReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess($"access project-policy delete {k_StatementId}", $"Given policy statements for project: '{CommonKeys.ValidProjectId}' and environment: '{CommonKeys.ValidEnvironmentId}' has been deleted");
    }

    // access player-policy delete
    [Test]
    public async Task AccessPlayerPolicyDeleteReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess($"access player-policy delete {AccessApiMock.PlayerId} {k_StatementId}", $"Given policy statements for player: '{AccessApiMock.PlayerId}' has been deleted");
    }

    [Test]
    public async Task AccessProjectPolicyDeleteWithNoStatementIdsThrowsRequiredArgumentMissingException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command("access project-policy delete")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    [Test]
    public async Task AccessPlayerPolicyDeleteWithNoStatementIdsThrowsRequiredArgumentMissingException()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await GetLoggedInCli()
            .Command($"access player-policy delete {AccessApiMock.PlayerId}")
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(k_RequiredArgumentMissing)
            .ExecuteAsync();
    }

    // helpers
    public static IEnumerable<string> AccessModuleCommands
    {
        get
        {
            yield return "access project-policy list";
            yield return $"access player-policy list --player-id {AccessApiMock.PlayerId}";
            yield return "access player-policy list";
            yield return $"access player-policy update {AccessApiMock.PlayerId} policy.json";
            yield return $"access project-policy delete {k_StatementId}";
            yield return $"access player-policy delete {AccessApiMock.PlayerId} {k_StatementId}";
        }
    }

    async Task AssertSuccess(string command, string? expectedStdErr = null, string? expectedStdOut = null)
    {
        var test = GetLoggedInCli()
            .Command(command);
        if (expectedStdErr != null)
        {
            test = test.AssertStandardErrorContains(expectedStdErr);
        }

        if (expectedStdOut != null)
        {
            test = test.AssertStandardOutputContains(expectedStdOut);
        }
        await test.ExecuteAsync();
    }

    async Task AssertException(string command, string expectedMessage)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMessage)
            .ExecuteAsync();
    }
}
