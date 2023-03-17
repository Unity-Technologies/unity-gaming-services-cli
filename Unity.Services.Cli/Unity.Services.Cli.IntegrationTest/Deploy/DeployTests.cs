using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.IntegrationTest.CloudCodeTests;
using Unity.Services.Cli.IntegrationTest.EnvTests;
using Unity.Services.Cli.IntegrationTest.RemoteConfigTests.Mock;

namespace Unity.Services.Cli.IntegrationTest.Deploy;

public class DeployTests : UgsCliFixture
{
    const string k_ConfigId = "config-id";

    static readonly string k_TestDirectory = Path.GetFullPath(Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir"));

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
            "foo",
            "Module.ccm",
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
    readonly IReadOnlyList<DeployTestCase> m_ReconcileTestCases = new[]
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
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "ready.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
    };
    readonly IReadOnlyList<DeployTestCase> m_DryRunDeployedTestCases = new[]
    {
        new DeployTestCase(
            "module.exports = () => {}\n module.exports.params = { sides: 'NUMERIC'};",
            "dry_script.js",
            "Cloud Code",
            0,
            "Loaded",
            "",
            k_TestDirectory),
        new DeployTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "dry_color.rc",
            "Remote Config",
            0,
            "",
            "",
            k_TestDirectory),
        new DeployTestCase(
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "dry_ready.rc",
            "Remote Config",
            0,
            "",
            "",
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

    List<DeployContent> m_DeployedContents = new();
    List<DeployContent> m_FailedContents = new();
    List<DeployContent> m_DryRunContents = new();

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
        m_RemoteConfigMock.MockDeleteConfigAsync(k_ConfigId);

        Directory.CreateDirectory(k_TestDirectory);

        var environmentModels = await IdentityV1MockServerModels.GetModels();
        m_RemoteConfigMock.MockApi.Server?.WithMapping(environmentModels.ToArray());

        var cloudCodeModels = await CloudCodeV1MockServerModels.GetModels("Script");
        m_RemoteConfigMock.MockApi.Server?.WithMapping(cloudCodeModels.ToArray());

        var cloudCodeModuleModels = await CloudCodeV1MockServerModels.GetModuleModels("Module");
        m_RemoteConfigMock.MockApi.Server?.WithMapping(cloudCodeModuleModels.ToArray());

        CloudCodeV1MockServerModels.OverrideListModules(m_RemoteConfigMock.MockApi);
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

            if (testCase.DeployedContent.Type == "Remote Config")
            {
                try
                {
                    var rcContents = RemoteConfigToDeployContents(testCase);

                    foreach (var rcContent in rcContents)
                    {
                        contents.Add(rcContent);
                    }

                    continue;
                }
                catch { }
            }

            contents.Add(testCase.DeployedContent);
        }
    }

    [Test]
    public async Task DeployInvalidPath()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        var invalidDirectory = Path.GetFullPath("invalid-directory");
        var expectedOutput = $"[Error]: {Environment.NewLine}"
            + $"    Path \"{invalidDirectory}\" could not be found.{Environment.NewLine}";

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
        var deployedConfigFileString = string.Join(Environment.NewLine + "    ", m_DeployedTestCases.Select(GetConfigFileString));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutputContains($"Deployed:{Environment.NewLine}    {deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployValidConfigWithOptionsSucceed()
    {
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        var deployedConfigFileString = string.Join(Environment.NewLine + "    ", m_DeployedTestCases.Select(GetConfigFileString));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName}")
            .AssertStandardOutputContains($"Deployed:{Environment.NewLine}    {deployedConfigFileString}")
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
    public async Task DeployConfigWithInvalidOnlyFailInvalid()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_DeployedTestCases, m_DeployedContents);
        await CreateDeployTestFilesAsync(m_FailedTestCases, m_FailedContents);
        var deployedConfigFileString = string.Join(Environment.NewLine + "    ", m_DeployedTestCases.Select(GetConfigFileString));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory}")
            .AssertStandardOutput(output =>
            {
                StringAssert.Contains($"Deployed:{Environment.NewLine}    {deployedConfigFileString}", output);
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
        var logResult = new JsonDeployLogResult(CreateResult
        (m_DeployedContents,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            m_DeployedContents,
            m_FailedContents));
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j")
            .AssertStandardOutputContains(JsonConvert.SerializeObject(logResult, Formatting.Indented))
            .AssertExitCode(ExitCode.HandledError)
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
                new DeployContent("example-string.js", "JS", null!),
                new DeployContent("example-string.js", "JS", null!),
                new DeployContent("example-string.js", "JS", null!),
                new DeployContent("ExistingModule.ccm", "JS", null!),
                new DeployContent("AnotherExistingModule.ccm", "JS", null!),
                new DeployContent("test", "Remote Config", "")
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

    static List<DeployContent> RemoteConfigToDeployContents(DeployTestCase testCase)
    {
        var deployContents = new List<DeployContent>();
        var json = JsonConvert.DeserializeObject<RemoteConfigFileContent>(testCase.ConfigValue);

        foreach (var kvp in json!.entries)
        {
            deployContents.Add(new DeployContent(kvp.Key,
                "Remote Config",
                testCase.ConfigFilePath,
                testCase.DeployedContent.Progress,
                testCase.DeployedContent.Status,
                testCase.DeployedContent.Detail));
        }

        return deployContents;
    }

    DeploymentResult CreateResult(
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
            var isRemoteConfig = createdOriginal.Type == "Remote Config";
            var progress = isRemoteConfig ? 0f : createdOriginal.Progress;
            var status = isRemoteConfig ? string.Empty : createdOriginal.Status;
            var detail = isRemoteConfig ? string.Empty : createdOriginal.Detail;

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

    string GetConfigFileString(DeployTestCase testCase)
    {
        if (testCase.DeployedContent.Type != "Remote Config")
        {
            return testCase.ConfigFileName;
        }

        var configFileString = string.Empty;
        var rcContent = JsonConvert.DeserializeObject<RemoteConfigFileContent>(testCase.ConfigValue);
        var keys = rcContent!.entries.Keys.ToList();

        for (var i = 0; i < keys.Count; i++)
        {
            configFileString += keys[i];

            if (i < keys.Count - 1)
            {
                configFileString += Environment.NewLine;
            }
        }

        return configFileString;
    }

    class RemoteConfigFileContent
    {
        public Dictionary<string, string> entries;
        public Dictionary<string, string> types;

        public RemoteConfigFileContent()
        {
            entries = new Dictionary<string, string>();
            types = new Dictionary<string, string>();
        }
    }
}
