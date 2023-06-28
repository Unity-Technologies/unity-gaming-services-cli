using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.RemoteConfig;

public class RemoteConfigDeployTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_DeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "color.rc",
            "RemoteConfig File",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory,
            SeverityLevel.Success),
        new AuthoringTestCase(
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "ready.rc",
            "RemoteConfig File",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory,
            SeverityLevel.Success)
    };

    readonly IReadOnlyList<AuthoringTestCase> m_ReconcileTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "color.rc",
            "RemoteConfig File",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory,
            SeverityLevel.Success),
        new AuthoringTestCase(
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "ready.rc",
            "RemoteConfig File",
            100,
            "Deployed",
            "Deployed Successfully",
            k_TestDirectory,
            SeverityLevel.Success),
    };

    readonly IReadOnlyList<AuthoringTestCase> m_DryRunDeployedTestCases = new[]
    {
        new AuthoringTestCase(
            "{ \"entries\" : { \"color\" : \"Red\" } }",
            "dry_color.rc",
            "RemoteConfig Entry",
            0,
            "",
            "",
            k_TestDirectory),
        new AuthoringTestCase(
            "{ \"entries\" : { \"Ready\" : \"True\" } }",
            "dry_ready.rc",
            "RemoteConfig Entry",
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
            "RemoteConfig File",
            0,
            "Failed To Read",
            "Invalid character after parsing property name. Expected ':' but got: T. Path 'entries', line 1, position 26.",
            k_TestDirectory,
            SeverityLevel.Error)
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
            .AssertStandardOutputContains($"Successfully deployed the following files:{Environment.NewLine}    {deployedConfigFileString}")
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
            .AssertStandardOutputContains($"Successfully deployed the following files:{Environment.NewLine}    {deployedConfigFileString}")
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
                StringAssert.Contains($"Successfully deployed the following files:{Environment.NewLine}    {deployedConfigFileString}", output);
                foreach (var failedTestCase in m_FailedTestCases)
                {
                    StringAssert.Contains($"Failed to deploy:{Environment.NewLine}    " +
                        $"'{failedTestCase.ConfigFilePath}' - Status: {failedTestCase.DeployedContent.Status.Message}", output);
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

        var createdEntries = m_DeployedTestCases
            .SelectMany(tc => RemoteConfigFileContent.RemoteConfigToDeployContents(
                tc ,
                new DeploymentStatus(Statuses.Created, string.Empty)))
            .ToList();

        var logResult = DeployTestsFixture.CreateResult(
            createdEntries,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            m_DeployedTestCases.Select(d => d.DeployedContent).ToList(),
            m_FailedTestCases.Select(d => d.DeployedContent).ToList());

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

        var dc = m_DryRunDeployedTestCases
            .SelectMany(tc => RemoteConfigFileContent.RemoteConfigToDeployContents(
                tc,
                new DeploymentStatus(Statuses.Created, string.Empty),
                100f))
            .ToList();

        var logResult = new DeploymentResult(
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            dc,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            true);
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

        var createdEntries = m_ReconcileTestCases
            .SelectMany(tc => RemoteConfigFileContent.RemoteConfigToDeployContents(tc, new DeploymentStatus(Statuses.Created, string.Empty)))
            .ToList();

        var logResult = DeployTestsFixture.CreateResult(
            createdEntries,
            Array.Empty<DeployContent>(),
            new[]
            {
                new DeployContent("test", "RemoteConfig Entry", "Remote", 100, "Deleted")
            },
            m_ReconcileTestCases.Select(tc => tc.DeployedContent).ToList(),
            Array.Empty<DeployContent>());

        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j --reconcile -s remote-config")
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

        var createdEntries = m_ReconcileTestCases
            .SelectMany(tc => RemoteConfigFileContent.RemoteConfigToDeployContents(tc, new DeploymentStatus(Statuses.Created, string.Empty)))
            .ToList();

        var logResult = DeployTestsFixture.CreateResult(
            createdEntries,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            m_ReconcileTestCases.Select(tc => tc.DeployedContent).ToList(),
            Array.Empty<DeployContent>());

        var resultString = JsonConvert.SerializeObject(logResult, Formatting.Indented);
        await GetLoggedInCli()
            .Command($"deploy {k_TestDirectory} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    static string GetConfigFileString(AuthoringTestCase testCase)
    {
        if (testCase.DeployedContent.Type != "Remote Config")
        {
            return $"'{testCase.ConfigFilePath}'";
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
