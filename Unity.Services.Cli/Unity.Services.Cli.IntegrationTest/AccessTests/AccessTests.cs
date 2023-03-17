using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.IntegrationTest.AccessTests.Mock;
using Unity.Services.Cli.IntegrationTest.EnvTests;
using Unity.Services.Cli.MockServer;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.IntegrationTest.AccessTests;

public class AccessTests: UgsCliFixture
{
    private const string k_PlayerId = "j0oM0dnufzxgtwGqoH0zIpSyWUV7XUgy";

    private readonly AccessApiMock m_AccessApiMock = new(CommonKeys.ValidProjectId, CommonKeys.ValidEnvironmentId, k_PlayerId);
    const string k_AccessOpenApiUrl
        = "https://services.docs.unity.com/specs/v1/616363657373.yaml";

    private const string k_FilePath = "policy.json";
    const string k_ProjectIdNotSetErrorMessage = "'project-id' is not set in project configuration."
                                                 + " '" + Keys.EnvironmentKeys.ProjectId + "' is not set in system environment variables.";
    const string k_LoggedOutErrorMessage = "You are not logged into any service account."
                                           + " Please login using the 'ugs login' command.";
    const string k_EnvironmentNameNotSetErrorMessage = "'environment-name' is not set in project configuration."
                                                       + " '" + Keys.EnvironmentKeys.EnvironmentName + "' is not set in system environment variables.";

    readonly string k_TestDirectory = Path.GetFullPath(Path.Combine(UgsCliBuilder.RootDirectory, "Unity.Services.Cli/Unity.Services.Cli.IntegrationTest/AccessTests/Data/"));

    private const string k_RequiredArgumentMissing = "Required argument missing for command";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        m_AccessApiMock.MockApi.InitServer();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_AccessApiMock.MockApi.Server?.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        m_AccessApiMock.MockApi.Server?.ResetMappings();

        var environmentModels = await IdentityV1MockServerModels.GetModels();
        m_AccessApiMock.MockApi.Server?.WithMapping(environmentModels.ToArray());

        var accessModels = await MappingModelUtils.ParseMappingModelsAsync(k_AccessOpenApiUrl, new());
        accessModels = accessModels.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId)
                .ConfigMappingPathWithKey("players", k_PlayerId)
        );
        m_AccessApiMock.MockApi.Server?.WithMapping(accessModels.ToArray());

    }

    [TearDown]
    public void TearDown()
    {
        m_AccessApiMock.MockApi.Server?.ResetMappings();
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
            .AssertStandardOutputContains(k_LoggedOutErrorMessage)
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
        m_AccessApiMock.MockGetProjectPolicy();
        await AssertSuccess("access get-project-policy", "\"statements\": []");
    }

    // access get-player-policy
    [Test]
    public async Task AccessGetPlayerPolicyReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        m_AccessApiMock.MockGetPlayerPolicy();

        var playerPolicy = new
        {
            playerId = k_PlayerId,
            statements = new List<Statement>()
        };

        await AssertSuccess($"access get-player-policy {k_PlayerId}", JsonConvert.SerializeObject(playerPolicy, Formatting.Indented));
    }

    // access get-all-player-policies
    [Test]
    public async Task AccessGetAllPlayerPoliciesReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        m_AccessApiMock.MockGetAllPlayerPolicies();

        var playerPolicy = new
        {
            playerId = k_PlayerId,
            statements = new List<Statement>()
        };

        List<object> obj = new List<object>();
        obj.Add(playerPolicy);

        await AssertSuccess("access get-all-player-policies", JsonConvert.SerializeObject(obj, Formatting.Indented));
    }

    // access upsert-project-policy
    [Test]
    public async Task AccessUpsertProjectPolicyReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        m_AccessApiMock.MockUpsertProjectPolicy();
        await AssertSuccess($"access upsert-project-policy {Path.Combine(k_TestDirectory, "policy.json")}", $"Policy for project: '{CommonKeys.ValidProjectId}' and environment: '{CommonKeys.ValidEnvironmentId}' has been updated");
    }

    // access upsert-player-policy
    [Test]
    public async Task AccessUpsertPlayerPolicyReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        m_AccessApiMock.MockUpsertPlayerPolicy();
        await AssertSuccess($"access upsert-player-policy {k_PlayerId} {Path.Combine(k_TestDirectory, "policy.json")}", $"Policy for player: '{k_PlayerId}' has been updated");
    }

    // access delete-project-policy-statements
    [Test]
    public async Task AccessDeleteProjectPolicyStatementsReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        m_AccessApiMock.MockDeleteProjectPolicyStatements();
        await AssertSuccess($"access delete-project-policy-statements {Path.Combine(k_TestDirectory, "statements.json")}", $"Given policy statements for project: '{CommonKeys.ValidProjectId}' and environment: '{CommonKeys.ValidEnvironmentId}' has been deleted");
    }

    // access delete-player-policy-statements
    [Test]
    public async Task AccessDeletePlayerPolicyStatementsReturnsZeroExitCode()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        m_AccessApiMock.MockDeletePlayerPolicyStatements();
        await AssertSuccess($"access delete-player-policy-statements {k_PlayerId} {Path.Combine(k_TestDirectory, "statements.json")}", $"Given policy statements for player: '{k_PlayerId}' has been deleted");
    }

    // helpers
    public static IEnumerable<string> AccessModuleCommands
    {
        get
        {
            yield return "access get-project-policy";
            yield return $"access get-player-policy {k_PlayerId}";
            yield return "access get-all-player-policies";
            yield return $"access upsert-project-policy {k_FilePath}";
            yield return $"access upsert-player-policy {k_PlayerId} {k_FilePath}";
            yield return $"access delete-project-policy-statements {k_FilePath}";
            yield return $"access delete-player-policy-statements {k_PlayerId} policy.json";
        }
    }

    private static async Task AssertSuccess(string command, string expectedMessage)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertNoErrors()
            .AssertStandardOutputContains(expectedMessage)
            .ExecuteAsync();
    }

    private static async Task AssertException(string command, string expectedMessage)
    {
        await GetLoggedInCli()
            .Command(command)
            .AssertExitCode(ExitCode.HandledError)
            .AssertStandardOutputContains(expectedMessage)
            .ExecuteAsync();
    }
}
