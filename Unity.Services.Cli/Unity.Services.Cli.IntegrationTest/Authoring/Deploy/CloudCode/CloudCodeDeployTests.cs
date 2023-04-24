using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.CloudCode;

public class CloudCodeDeployTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_DeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            $"{CloudCodeV1Mock.ValidScriptName}.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory),
    };
    readonly IReadOnlyList<AuthoringTestCase> m_ReconcileTestCases = new[]
    {
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            $"{CloudCodeV1Mock.ValidScriptName}.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory)
    };

    readonly IReadOnlyList<AuthoringTestCase> m_DryRunDeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "dry_script.js",
            "Cloud Code",
            0,
            "Loaded",
            "",
            k_TestDirectory),
    };

    List<DeployContent> m_DeployedContents = new();
    List<DeployContent> m_FailedContents = new();
    List<DeployContent> m_DryRunContents = new();

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

        Directory.CreateDirectory(k_TestDirectory);
        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        await m_MockApi.MockServiceAsync(new CloudCodeV1Mock());
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

    [Test]
    public async Task DeployValidConfigFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var deployedConfigFileString = string.Join(Environment.NewLine + "    ", m_DeployedTestCases.Select(a=>a.ConfigFileName));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"Deployed:{Environment.NewLine}    {deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployValidConfigFromDirectorySucceedWithJsonOutput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var logResult = new JsonDeployLogResult(CreateResult(
            m_DeployedContents,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            m_DeployedContents,
            Array.Empty<DeployContent>()));
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployConfig_DryRun()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DryRunDeployedTestCases, m_DryRunContents);

        var logResult = new JsonDeployLogResult(new DeploymentResult(
            m_DryRunContents,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            true));
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j --dry-run")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithReconcileWillDeleteRemoteFiles()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_ReconcileTestCases, m_DeployedContents);

        var logResult = new JsonDeployLogResult(CreateResult(
            m_DeployedContents,
            Array.Empty<DeployContent>(),
            new[]
            {
                new DeployContent("example-string.js", "JS", ""),
                new DeployContent("example-string.js", "JS", ""),
                new DeployContent("example-string.js", "JS", ""),
                new DeployContent("ExistingModule.ccm", "JS", ""),
                new DeployContent("AnotherExistingModule.ccm", "JS", ""),
            },
            m_DeployedContents,
            Array.Empty<DeployContent>()));
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j --reconcile")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithoutReconcileWillNotDeleteRemoteFiles()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_ReconcileTestCases, m_DeployedContents);

        var logResult = new JsonDeployLogResult(CreateResult(
            m_DeployedContents,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            m_DeployedContents,
            Array.Empty<DeployContent>()));
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    static async Task CreateDeployTestFilesAsync(IReadOnlyList<AuthoringTestCase> testCases, ICollection<DeployContent> contents)
    {
        foreach (var testCase in testCases)
        {
            await File.WriteAllTextAsync(testCase.ConfigFilePath, testCase.ConfigValue);

            contents.Add(testCase.DeployedContent);
        }
    }

    static DeploymentResult CreateResult(
        IReadOnlyCollection<DeployContent> created,
        IReadOnlyCollection<DeployContent> updated,
        IReadOnlyCollection<DeployContent> deleted,
        IReadOnlyCollection<DeployContent> deployed,
        IReadOnlyCollection<DeployContent> failed,
        bool dryRun = false)
    {
        var createdCopy = new List<DeployContent>();

        foreach (var createdOriginal in created)
        {
            var progress = createdOriginal.Progress;
            var status = createdOriginal.Status;
            var detail = createdOriginal.Detail;

            createdCopy.Add(new DeployContent(
                createdOriginal.Name,
                createdOriginal.Type,
                createdOriginal.Path,
                progress,
                status,
                detail));
        }

        return new DeploymentResult(createdCopy, updated, deleted, deployed, failed, dryRun);
    }
}
