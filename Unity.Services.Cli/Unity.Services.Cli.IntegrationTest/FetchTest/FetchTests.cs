using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.IntegrationTest.Deploy;
using Unity.Services.Cli.IntegrationTest.EnvTests;
using Unity.Services.Cli.IntegrationTest.RemoteConfigTests.Mock;
using Unity.Services.Cli.MockServer;
using WireMock.Admin.Mappings;

namespace Unity.Services.Cli.IntegrationTest.FetchTest;

public class FetchTests : UgsCliFixture
{
    const string k_ConfigId = "config-id";

    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir");

    readonly IReadOnlyList<DeployTestCase> m_DeployedTestCases = new[]
    {
        new DeployTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "color.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
        new DeployTestCase(
            "{ \"entries\" : { \"ready\" : \"True\" } }",
            "ready.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory)
    };

    readonly IReadOnlyList<DeployTestCase> m_FailedTestCases = new[]
    {
        new DeployTestCase(
            "{ \"entries\" : { \"Ready : \"True\" } }",
            "invalid1.rc",
            "Remote Config",
            0,
            "Failed To Read",
            "Invalid character after parsing property name. Expected ':' but got: T. Path 'entries', line 1, position 26.",
            k_TestDirectory)
    };

    // Since Remote Config open api is having issues we should use our mock to map the models for now
    readonly RemoteConfigMock m_RemoteConfigMock = new(CommonKeys.ValidProjectId, CommonKeys.ValidEnvironmentId);

    IEnumerable<MappingModel>? m_CloudCodeModels;

    const string k_CloudCodeOpenApiUrl = "https://services.docs.unity.com/specs/v1/636c6f75642d636f64652d61646d696e.yaml";

    List<DeployContent> m_DeployedContents = new();
    List<DeployContent> m_FailedContents = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        m_RemoteConfigMock.MockApi.InitServer();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_RemoteConfigMock.MockApi.Server?.Dispose();
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

        m_DeployedContents.Clear();
        m_FailedContents.Clear();
        m_RemoteConfigMock.MockApi.Server?.ResetMappings();

        m_RemoteConfigMock.MockGetAllConfigsFromEnvironmentAsync(k_ConfigId);
        m_RemoteConfigMock.MockUpdateConfigAsync(k_ConfigId);

        Directory.CreateDirectory(k_TestDirectory);

        var environmentModels = await IdentityV1MockServerModels.GetModels();

        m_RemoteConfigMock.MockApi.Server?.WithMapping(environmentModels.ToArray());

        m_CloudCodeModels = await MappingModelUtils.ParseMappingModelsAsync(
            k_CloudCodeOpenApiUrl,
            new());
        m_CloudCodeModels = m_CloudCodeModels.Select(
            m => m.ConfigMappingPathWithKey(CommonKeys.ProjectIdKey, CommonKeys.ValidProjectId)
                .ConfigMappingPathWithKey(CommonKeys.EnvironmentIdKey, CommonKeys.ValidEnvironmentId));

        MapCloudCodeModels();
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

    static async Task CreateDeployTestFilesAsync(IReadOnlyList<DeployTestCase> testCases, ICollection<DeployContent> contents)
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
        var expectedOutput = $"[Error]: {Environment.NewLine}"
                             + $"    Path \"{invalidDirectory}\" could not be found.{Environment.NewLine}";

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
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);

        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory}")
            .AssertStandardOutput(output =>
            {
                StringAssert.Contains($"Successfully fetched into the following files:{Environment.NewLine}", output);
                foreach (var file in m_DeployedTestCases)
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
            .AssertStandardOutputContains($"No content fetched")
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
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var fetchedPaths = m_DeployedTestCases
            .Select(t => Path.GetRelativePath(UgsCliBuilder.RootDirectory, t.ConfigFilePath))
            .ToArray();

        var deletedKeys = m_DeployedTestCases
            .Select(t => $"Key '{Path.GetFileNameWithoutExtension(t.ConfigFileName)}' " +
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

    void MapCloudCodeModels()
    {
        var cloudCodeModels = m_CloudCodeModels as MappingModel[] ?? m_CloudCodeModels?.ToArray();
        cloudCodeModels = cloudCodeModels?.Select(m => m.ConfigMappingPathWithKey("scripts", "Script")).ToArray();
        m_RemoteConfigMock.MockApi.Server?.WithMapping(cloudCodeModels ?? Array.Empty<MappingModel>());
    }
}

