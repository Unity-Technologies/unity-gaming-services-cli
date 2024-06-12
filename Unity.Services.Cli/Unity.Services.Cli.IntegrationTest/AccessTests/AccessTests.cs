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
    const string k_FilePath = "policy.json";
    const string k_ProjectIdNotSetErrorMessage = "'project-id' is not set in project configuration."
                                                 + " '" + Keys.EnvironmentKeys.ProjectId + "' is not set in system environment variables.";
    const string k_LoggedOutErrorMessage = "You are not logged into any service account."
                                           + " Please login using the 'ugs login' command.";
    const string k_EnvironmentNameNotSetErrorMessage = "'environment-name' is not set in project configuration."
                                                       + " '" + Keys.EnvironmentKeys.EnvironmentName + "' is not set in system environment variables.";

    readonly string m_TestDirectory = Path.GetFullPath(Path.Combine(UgsCliBuilder.RootDirectory, "Unity.Services.Cli/Unity.Services.Cli.IntegrationTest/AccessTests/Data/"));

    const string k_RequiredArgumentMissing = "Required argument missing for command";

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

        await new UgsCliTestCase()
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

    [TestCase("access get-player-policy")]
    [TestCase("access upsert-player-policy")]
    [TestCase("access delete-player-policy-statements")]
    [TestCase("access upsert-player-policy XLwhjNi96BklAvFY")]
    [TestCase("access delete-player-policy-statements XLwhjNi96BklAvFY")]
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

    // access get-project-policy
    [Test]
    public async Task AccessGetProjectPolicyReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess("access get-project-policy", expectedStdOut: "statement-1");
    }

    // access get-player-policy
    [Test]
    public async Task AccessGetPlayerPolicyReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        var playerPolicy = new
        {
            playerId = AccessApiMock.PlayerId,
            statements = new List<PlayerStatement>()
        };

        await AssertSuccess($"access get-player-policy {AccessApiMock.PlayerId}", expectedStdOut: JsonConvert.SerializeObject(playerPolicy, Formatting.Indented));
    }

    // access get-all-player-policies
    [Test]
    public async Task AccessGetAllPlayerPoliciesReturnsZeroExitCode()
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

        await AssertSuccess("access get-all-player-policies", expectedStdOut: JsonConvert.SerializeObject(obj, Formatting.Indented));
    }

    // access upsert-project-policy
    [Test]
    public async Task AccessUpsertProjectPolicyReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess($"access upsert-project-policy {Path.Combine(m_TestDirectory, "policy.json")}", $"Policy for project: '{CommonKeys.ValidProjectId}' and environment: '{CommonKeys.ValidEnvironmentId}' has been updated");
    }

    // access upsert-player-policy
    [Test]
    public async Task AccessUpsertPlayerPolicyReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess($"access upsert-player-policy {AccessApiMock.PlayerId} {Path.Combine(m_TestDirectory, "policy.json")}", $"Policy for player: '{AccessApiMock.PlayerId}' has been updated");
    }

    // access delete-project-policy-statements
    [Test]
    public async Task AccessDeleteProjectPolicyStatementsReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess($"access delete-project-policy-statements {Path.Combine(m_TestDirectory, "statements.json")}", $"Given policy statements for project: '{CommonKeys.ValidProjectId}' and environment: '{CommonKeys.ValidEnvironmentId}' has been deleted");
    }

    // access delete-player-policy-statements
    [Test]
    public async Task AccessDeletePlayerPolicyStatementsReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await AssertSuccess($"access delete-player-policy-statements {AccessApiMock.PlayerId} {Path.Combine(m_TestDirectory, "statements.json")}", $"Given policy statements for player: '{AccessApiMock.PlayerId}' has been deleted");
    }

    // helpers
    public static IEnumerable<string> AccessModuleCommands
    {
        get
        {
            yield return "access get-project-policy";
            yield return $"access get-player-policy {AccessApiMock.PlayerId}";
            yield return "access get-all-player-policies";
            yield return $"access upsert-project-policy {k_FilePath}";
            yield return $"access upsert-player-policy {AccessApiMock.PlayerId} {k_FilePath}";
            yield return $"access delete-project-policy-statements {k_FilePath}";
            yield return $"access delete-player-policy-statements {AccessApiMock.PlayerId} policy.json";
        }
    }

    static async Task AssertSuccess(string command, string? expectedStdErr = null, string? expectedStdOut = null)
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

    static async Task AssertException(string command, string expectedMessage)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardErrorContains(expectedMessage)
            .ExecuteAsync();
    }
}
