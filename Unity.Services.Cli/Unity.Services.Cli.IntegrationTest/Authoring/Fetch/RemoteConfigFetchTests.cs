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

namespace Unity.Services.Cli.IntegrationTest.Authoring.Fetch;

public class RemoteConfigFetchTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_FetchedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "color.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
        new AuthoringTestCase(
            "{ \"entries\" : { \"ready\" : \"True\" } }",
            "ready.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory)
    };

    // Since Remote Config open api is having issues we should use our mock to map the models for now
    readonly List<DeployContent> m_FetchedContents = new();

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
        m_MockApi.Server?.ResetMappings();

        Directory.CreateDirectory(k_TestDirectory);

        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
        await m_MockApi.MockServiceAsync(new RemoteConfigMock());
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
            .Command($"fetch {invalidDirectory}")
            .AssertStandardOutputContains(expectedOutput)
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
            .Command($"fetch {k_TestDirectory}")
            .AssertStandardOutput(
                output =>
                {
                    Console.WriteLine(output);
                    StringAssert.Contains($"Successfully fetched into the following files:{Environment.NewLine}", output);
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
            .Command($"fetch {k_TestDirectory}")
            .AssertStandardOutputContains("No content fetched")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchNoConfigFromDirectoryWithOptionSucceed()
    {
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName}")
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
            .Select(t => Path.GetRelativePath(UgsCliBuilder.RootDirectory, t.ConfigFilePath))
            .ToArray();

        var deletedKeys = m_FetchedTestCases
            .Select(
                t => $"Key '{Path.GetFileNameWithoutExtension(t.ConfigFileName)}' " +
                    $"in file '{Path.GetFullPath(t.ConfigFilePath)}'")
            .ToArray();

        var logResult = new
        {
            Result = new FetchResult(
                Array.Empty<string>(),
                deletedKeys,
                Array.Empty<string>(),
                fetchedPaths,
                Array.Empty<string>()),
            Messages = Array.Empty<string>()
        };
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
