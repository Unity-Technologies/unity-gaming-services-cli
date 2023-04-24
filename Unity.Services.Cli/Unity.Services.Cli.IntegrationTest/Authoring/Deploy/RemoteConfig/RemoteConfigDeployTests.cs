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

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.RemoteConfig;

public class RemoteConfigDeployTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_DeployedTestCases = new[]
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
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "ready.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
    };

    readonly IReadOnlyList<AuthoringTestCase> m_ReconcileTestCases = new[]
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
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "ready.rc",
            "Remote Config",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory),
    };

    readonly IReadOnlyList<AuthoringTestCase> m_DryRunDeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "dry_color.rc",
            "Remote Config",
            0,
            "",
            "",
            k_TestDirectory),
        new AuthoringTestCase(
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "dry_ready.rc",
            "Remote Config",
            0,
            "",
            "",
            k_TestDirectory),
    };

    readonly IReadOnlyList<AuthoringTestCase> m_FailedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"entries\" : { \"Ready : \"True\" } }",
            "invalid1.rc",
            "Remote Config",
            0,
            "Failed To Read",
            "Invalid character after parsing property name. Expected ':' but got: T. Path 'entries', line 1, position 26.",
            k_TestDirectory)
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
        await m_MockApi.MockServiceAsync(new RemoteConfigMock());
    }

    static async Task CreateDeployTestFilesAsync(IReadOnlyList<AuthoringTestCase> testCases, ICollection<DeployContent> contents)
    {
        foreach (var testCase in testCases)
        {
            await File.WriteAllTextAsync(testCase.ConfigFilePath, testCase.ConfigValue);

            if (testCase.DeployedContent.Type == "Remote Config")
            {
                try
                {
                    var rcContents = RemoteConfigFileContent.RemoteConfigToDeployContents(testCase);

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
                new DeployContent("test", "Remote Config", "Remote")
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

    private static DeploymentResult CreateResult(
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
            var progress = 0f;
            var status = string.Empty;
            var detail = string.Empty;

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

    string GetConfigFileString(AuthoringTestCase testCase)
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
}
