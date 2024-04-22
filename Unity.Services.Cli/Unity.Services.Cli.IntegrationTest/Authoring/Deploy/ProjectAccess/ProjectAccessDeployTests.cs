using System.IO;
using System.Linq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.ProjectAccess;

[Ignore("It's breaking the CI")]
public class ProjectAccessDeployTests : UgsCliFixture
{

    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp");

    readonly IReadOnlyList<AuthoringTestCase> m_DeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"Statements\": [ {\"Sid\": \"allow-access-to-economy\",\"Action\": [ \"Read\"],\"Effect\": \"Allow\",\"Principal\": \"Player\",\"Resource\": \"urn:ugs:economy:*\",\"ExpiresAt\": \"2024-04-29T18:30:51.243Z\",\"Version\": \"10.0\" } ]}",
            "statements.ac",
            "Access File",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory,
            SeverityLevel.Success),
    };

    readonly List<DeployContent> m_DeployedContents = new();
    readonly List<DeployContent> m_FailedContents = new();

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }

        m_DeployedContents.Clear();
        m_FailedContents.Clear();
        MockApi.Server?.ResetMappings();

        Directory.CreateDirectory(k_TestDirectory);

        await MockApi.MockServiceAsync(new AccessApiMock());
        await MockApi.MockServiceAsync(new RemoteConfigMock());
        await MockApi.MockServiceAsync(new LeaderboardApiMock());
        await MockApi.MockServiceAsync(new IdentityV1Mock());
    }


    [TearDown]
    public void TearDown()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }
    }

    static async Task CreateDeployTestFilesAsync(IReadOnlyList<AuthoringTestCase> testCases, ICollection<DeployContent> contents)
    {
        foreach (var testCase in testCases)
        {
            await File.WriteAllTextAsync(testCase.ConfigFilePath, testCase.ConfigValue);
            contents.Add(testCase.DeployedContent);
        }
    }

    [Test]
    public async Task DeployValidConfigFromDirectoryWithOptionSucceed()
    {
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var deployedConfigFileString = string.Join(System.Environment.NewLine + "    ", m_DeployedTestCases.Select(r => $"'{r.ConfigFilePath}'"));

        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName}")
            .AssertStandardOutputContains($"Successfully deployed the following files:{System.Environment.NewLine}    {deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployValidConfigFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var deployedConfigFileString = string.Join(System.Environment.NewLine + "    ", m_DeployedTestCases.Select(r => $"'{r.ConfigFilePath}'"));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"Successfully deployed the following files:{System.Environment.NewLine}    {deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployValidConfigDryRunSucceed()
    {
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var deployedConfigFileString = string.Join(System.Environment.NewLine + "    ", m_DeployedTestCases.Select(r => $"'{r.ConfigFilePath}'"));

        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName} --dry-run")
            .AssertStandardOutputContains($"Will deploy following files:{System.Environment.NewLine}    {deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }


    [Test]
    public async Task DeployInvalidPath()
    {
        var invalidDirectory = Path.GetFullPath("invalid-directory");
        var expectedOutput = $"Path \"{invalidDirectory}\" could not be found.";

        await GetLoggedInCli()
            .Command($"deploy {invalidDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName}")
            .AssertStandardErrorContains(expectedOutput)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }
}
