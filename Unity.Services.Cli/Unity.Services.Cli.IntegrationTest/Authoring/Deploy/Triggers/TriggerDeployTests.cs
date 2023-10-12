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

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.Triggers;

[Ignore("These tests pass individually but fail when run as part of a batch. Since it doesn't seem to be an issue with prod code, ignoring for now - DM")]
public class TriggersDeployTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_DeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "{\"Configs\":[{\"Name\":\"Trigger1\",\"EventType\":\"EventType1\",\"ActionType\":\"cloud-code\",\"ActionUrn\":\"ActionUrn1\"}]}",
            "Triggers1.tr",
            "Trigger",
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
        MockApi.Server?.ResetMappings();
        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new TriggersApiMock());
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
        await GetFullySetCli()
            .DebugCommand($"deploy {k_TestDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName} -s triggers")
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
        await GetFullySetCli()
            .DebugCommand($"deploy {k_TestDirectory} -s triggers")
            .AssertStandardOutputContains($"Successfully deployed the following files:{System.Environment.NewLine}    {deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
