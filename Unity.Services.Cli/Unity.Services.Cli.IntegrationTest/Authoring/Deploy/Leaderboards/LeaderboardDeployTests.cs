using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.Leaderboards;

/*
 * This is a temp file, there's a lot of common code with DeployTests, just to include leaderboard deploy command to the integration tests.
 * Since leaderboard deploy is hiding behind a feature, a separate test file will be easier to be excluded by feature flag.
 */
public class LeaderboardDeployTests : UgsCliFixture
{

    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_DeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"id\": \"lb1\", \"name\": \"foo\", \"sortOrder\": 1, \"updateType\": 1, \"created\": \"2023-01-01\", \"updated\": \"2023-01-01\"}",
            "leaderboard.lb",
            "Leaderboard",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
    };

    List<DeployContent> m_DeployedContents = new();
    List<DeployContent> m_FailedContents = new();

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
        m_MockApi.Server?.ResetMappings();
        await m_MockApi.MockServiceAsync(new LeaderboardApiMock());
        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        Directory.CreateDirectory(k_TestDirectory);
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
}
