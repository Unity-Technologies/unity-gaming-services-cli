using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.EnvTests;
using Unity.Services.Cli.IntegrationTest.RemoteConfigTests.Mock;
using Unity.Services.Cli.MockServer;
using WireMock.Admin.Mappings;

namespace Unity.Services.Cli.IntegrationTest.Deploy;

public class DeployTests : UgsCliFixture
{
    const string k_ConfigId = "config-id";

    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir");

    readonly IReadOnlyList<DeployTestCase> m_DeployedTestCases = new[]
    {
        new DeployTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "Script.js",
            "Cloud Code",
            100,
            "Up to date",
            "",
            k_TestDirectory),
        new DeployTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "color.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
        new DeployTestCase(
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "ready.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
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


        m_CloudCodeModels = await MappingModelUtils.ParseMappingModelsAsync(k_CloudCodeOpenApiUrl, new());
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
    public async Task DeployInvalidPath()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        const string invalidDirectory = "invalid-directory";
        var expectedOutput = $"[Error]: {Environment.NewLine}"
                             + $"    Path {invalidDirectory} could not be found.{Environment.NewLine}";

        await GetLoggedInCli()
            .Command($"deploy {invalidDirectory}")
            .AssertStandardOutputContains(expectedOutput)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithoutEnvironmentConfig()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"[Error]: {Environment.NewLine}    'environment-name' is not set")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithoutProjectConfig()
    {
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"[Error]: {Environment.NewLine}    'project-id' is not set")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployWithoutLogin()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

        await new UgsCliTestCase()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"[Error]: {Environment.NewLine}    You are not logged into any service account. Please login using the 'ugs login' command.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployValidConfigFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var deployedConfigFileString = string.Join(Environment.NewLine + "    ", m_DeployedTestCases.Select(r => r.ConfigFileName));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"Successfully deployed the following contents:{Environment.NewLine}    {deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployNoConfigFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"No content deployed")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployValidConfigFromDirectorySucceedWithJsonOutput()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var logResult = new JsonDeployLogResult(new DeploymentResult(m_DeployedContents, new List<DeployContent>()));
        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployConfigWithInvalidOnlyFailInvalid()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        await CreateDeployTestFilesAsync(m_FailedTestCases, m_FailedContents);
        var deployedConfigFileString = string.Join(Environment.NewLine + "    ", m_DeployedTestCases.Select(r => r.ConfigFileName));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutput(output =>
            {
                StringAssert.Contains($"Successfully deployed the following contents:{Environment.NewLine}    {deployedConfigFileString}", output);
                foreach (var failedTestCase in m_FailedTestCases)
                {
                    StringAssert.Contains($"Failed to deploy:{Environment.NewLine}    " +
                        $"'{failedTestCase.ConfigFileName}' - Status: {failedTestCase.DeployedContent.Status}", output);
                }
            })
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployConfigWithInvalidWithJsonOutputOnlyFailInvalid()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        await CreateDeployTestFilesAsync(m_FailedTestCases, m_FailedContents);
        var logResult = new JsonDeployLogResult(new DeploymentResult(m_DeployedContents, m_FailedContents));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j")
            .AssertStandardOutputContains(JsonConvert.SerializeObject(logResult, Formatting.Indented))
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }


    void MapCloudCodeModels()
    {
        var cloudCodeModels = m_CloudCodeModels as MappingModel[] ?? m_CloudCodeModels?.ToArray();
        cloudCodeModels = cloudCodeModels?.Select(m => m.ConfigMappingPathWithKey("scripts", "Script")).ToArray();
        m_RemoteConfigMock.MockApi.Server?.WithMapping(cloudCodeModels ?? Array.Empty<MappingModel>());
    }
}

