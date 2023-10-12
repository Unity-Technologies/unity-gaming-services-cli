using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Fetch;

public class RemoteConfigFetchTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_FetchedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "color.rc",
            "RemoteConfig File",
            100,
            "Fetched",
            null!,
            k_TestDirectory,
            SeverityLevel.Success),
        new AuthoringTestCase(
            "{ \"entries\" : { \"ready\" : \"True\" } }",
            "ready.rc",
            "RemoteConfig File",
            100,
            "Fetched",
            null!,
            k_TestDirectory,
            SeverityLevel.Success)
    };

    //do this
    readonly IReadOnlyList<DeployContent> m_FetchedKeysTestCases;

    // Since Remote Config open api is having issues we should use our mock to map the models for now
    readonly List<DeployContent> m_FetchedContents = new();

    public RemoteConfigFetchTests()
    {
        m_FetchedKeysTestCases = new[]
        {
            new CliRemoteConfigEntry("color" , "RemoteConfig Key", m_FetchedTestCases[0].ConfigFilePath, 100f, Statuses.Deleted, string.Empty),
            new CliRemoteConfigEntry("ready" , "RemoteConfig Key", m_FetchedTestCases[1].ConfigFilePath, 100f, Statuses.Deleted, string.Empty)
        };
    }

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }

        m_FetchedContents.Clear();
        MockApi.Server?.ResetMappings();

        Directory.CreateDirectory(k_TestDirectory);

        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new RemoteConfigMock());
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
    public async Task FetchInvalidPath()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        var invalidDirectory = Path.GetFullPath("invalid-directory");
        var expectedOutput = $"Path \"{invalidDirectory}\" could not be found.";

        await GetLoggedInCli()
            .Command($"fetch {invalidDirectory} -s remote-config")
            .AssertStandardErrorContains(expectedOutput)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchValidConfigFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_FetchedTestCases, m_FetchedContents);

        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} -s remote-config")
            .AssertStandardOutput(
                output =>
                {
                    Console.WriteLine(output);
                    StringAssert.Contains($"Successfully fetched the following files:{Environment.NewLine}", output);
                    foreach (var file in m_FetchedTestCases)
                    {
                        StringAssert.Contains(file.ConfigFileName, output);
                    }
                }).AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchNoConfigFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} -s remote-config")
            .AssertStandardOutputContains("No content fetched")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchNoConfigFromDirectoryWithOptionSucceed()
    {
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName} -s remote-config")
            .AssertStandardOutputContains($"No content fetched")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchValidConfigFromDirectorySucceedWithJsonOutput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_FetchedTestCases, m_FetchedContents);
        var fetchedPaths = m_FetchedTestCases
            .Select(t => t.DeployedContent)
            .ToArray();

        var logResult = new RemoteConfigFetchResult(
            Array.Empty<DeployContent>(),
            m_FetchedKeysTestCases,
            Array.Empty<DeployContent>(),
            fetchedPaths,
            Array.Empty<DeployContent>(),
            false);
        var resultString = JsonConvert.SerializeObject(logResult.ToTable(), Formatting.Indented);
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} -j -s remote-config")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
