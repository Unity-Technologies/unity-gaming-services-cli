using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.MockServer.ServiceMocks.CloudCode;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Fetch;

public class CloudCodeFetchTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly List<DeployContent> m_FetchedContents = new();

    static readonly IReadOnlyList<AuthoringTestCase> k_UpdatedTestCases = new[]
    {
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "remoteScript1.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory),
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "remoteScript2.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory),
    };

    static readonly IReadOnlyList<AuthoringTestCase> k_DeletedTestCases = new[]
    {
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "remoteScript4.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory),
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "remoteScript5.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory),
    };

    static readonly IReadOnlyList<AuthoringTestCase> k_CreatedTestCases = new[]
    {
        new AuthoringTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "remoteScript3.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory)
    };

    static readonly IReadOnlyList<AuthoringTestCase> k_FetchedTestCases = new List<AuthoringTestCase>(k_UpdatedTestCases.Concat(k_DeletedTestCases));

    static readonly IReadOnlyList<AuthoringTestCase> k_FetchedReconcileTestCases = new List<AuthoringTestCase>(k_CreatedTestCases.Concat(k_FetchedTestCases));

    [SetUp]
    public void CleanUpTestConfiguration()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }
        Directory.CreateDirectory(k_TestDirectory);
        m_FetchedContents.Clear();
    }

    [SetUp]
    public async Task SetUp()
    {
        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        await m_MockApi.MockServiceAsync(new CloudCodeFetchMock());
    }

    [TearDown]
    public void TearDown()
    {
        m_MockApi.Server?.ResetMappings();
    }

    static async Task CreateDeployTestFilesAsync(
        IReadOnlyList<AuthoringTestCase> testCases, ICollection<DeployContent> contents)
    {
        foreach (var testCase in testCases)
        {
            await File.WriteAllTextAsync(testCase.ConfigFilePath, testCase.ConfigValue);
            contents.Add(testCase.DeployedContent);
        }
    }

    [Test]
    public async Task FetchNoConfigFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory}")
            .AssertStandardOutputContains("No content fetched")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [TestCase("")]
    [TestCase("--dry-run")]
    public async Task FetchValidConfigFromDirectorySucceedWithJsonOutput(string dryRunOption)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(k_FetchedTestCases, m_FetchedContents);
        var fetchedPaths = k_FetchedTestCases
            .Select(t => t.ConfigFileName)
            .ToArray();
        var deletedPaths = k_DeletedTestCases
            .Select(t => t.ConfigFileName)
            .ToArray();
        var updatedPaths = k_UpdatedTestCases
            .Select(t => t.ConfigFileName)
            .ToArray();
        var logResult = new
        {
            Result = new FetchResult(
                updatedPaths,
                deletedPaths,
                Array.Empty<string>(),
                fetchedPaths,
                Array.Empty<string>()),
            Messages = Array.Empty<string>()
        };
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} {dryRunOption} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [TestCase("")]
    [TestCase("--dry-run")]
    public async Task FetchValidConfigReconcileSucceedWithJsonOutput(string dryRunOption)
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(k_FetchedTestCases, m_FetchedContents);
        var fetchedPaths = k_FetchedReconcileTestCases
            .Select(t => t.ConfigFileName)
            .ToArray();
        var deletedPaths = k_DeletedTestCases
            .Select(t => t.ConfigFileName)
            .ToArray();
        var updatedPaths = k_UpdatedTestCases
            .Select(t => t.ConfigFileName)
            .ToArray();
        var createdPaths = k_CreatedTestCases.Select(t => t.ConfigFileName).ToArray();

        var logResult = new
        {
            Result = new FetchResult(
                updatedPaths,
                deletedPaths,
                createdPaths,
                fetchedPaths,
                Array.Empty<string>()),
            Messages = Array.Empty<string>()
        };
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} --reconcile {dryRunOption} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
