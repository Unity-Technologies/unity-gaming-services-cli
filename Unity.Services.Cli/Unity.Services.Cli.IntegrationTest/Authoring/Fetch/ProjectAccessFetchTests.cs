using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Fetch;

[Ignore("It's breaking the CI")]
public class ProjectAccessFetchTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    readonly IReadOnlyList<AuthoringTestCase> m_FetchedTestCases = new[]
    {
        new AuthoringTestCase(
            "{\"statements\":[{\"Sid\":\"Statement-to-be-deleted\",\"Action\":[\"*\"],\"Resource\":\"urn:ugs:cloud-code:*\",\"Principal\":\"Player\",\"Effect\":\"Deny\"}]}",
            "project-statements.ac",
            "Access File",
            100,
            "Fetched",
            null!,
            k_TestDirectory,
            SeverityLevel.Success)
    };

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
        MockApi.Server?.ResetMappings();

        Directory.CreateDirectory(k_TestDirectory);

        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new AccessApiMock());
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
        IReadOnlyList<AuthoringTestCase> testCases ,ICollection<DeployContent> contents)
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
            .AssertStandardErrorContains(expectedOutput)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchValidConfigFromDirectorySucceedWithReconcile()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_FetchedTestCases, m_FetchedContents);

        await GetLoggedInCli()
            .Command($"fetch --services access {k_TestDirectory} --reconcile")
            .AssertStandardOutput(
                output =>
                {
                    Console.WriteLine(output);
                    StringAssert.Contains($"Successfully fetched the following files:{Environment.NewLine}", output);
                    StringAssert.Contains($"Created:{Environment.NewLine}", output);
                    StringAssert.Contains($"'statement-1' in '{k_TestDirectory}/project-statements.ac'", output);
                    StringAssert.Contains($"Deleted:{Environment.NewLine}", output);
                    StringAssert.Contains($"'Statement-to-be-deleted' in '{k_TestDirectory}/project-statements.ac'", output);
                    foreach (var file in m_FetchedTestCases)
                    {
                        StringAssert.Contains(file.ConfigFileName, output);
                    }
                }).AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchValidConfigFromDirectorySucceedWithoutReconcile()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_FetchedTestCases, m_FetchedContents);

        await GetLoggedInCli()
            .Command($"fetch --services access {k_TestDirectory}")
            .AssertStandardOutput(
                output =>
                {
                    Console.WriteLine(output);
                    StringAssert.Contains($"Successfully fetched the following files:{Environment.NewLine}", output);
                    StringAssert.Contains($"Deleted:{Environment.NewLine}", output);
                    StringAssert.Contains($"'Statement-to-be-deleted' in '{k_TestDirectory}/project-statements.ac'", output);
                    StringAssert.DoesNotContain($"Created:{Environment.NewLine}", output);
                    StringAssert.DoesNotContain($"'statement-1' in '{k_TestDirectory}/project-statements.ac'", output);
                    foreach (var file in m_FetchedTestCases)
                    {
                        StringAssert.Contains(file.ConfigFileName, output);
                    }
                }).AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchValidConfigFromDirectorySucceedWithDryRun()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_FetchedTestCases, m_FetchedContents);

        await GetLoggedInCli()
            .Command($"fetch --services access {k_TestDirectory} --dry-run")
            .AssertStandardOutput(
                output =>
                {
                    Console.WriteLine(output);
                    StringAssert.Contains($"This is a Dry Run. The result below is the expected result for this operation.{Environment.NewLine}", output);
                    StringAssert.Contains($"Will delete:{Environment.NewLine}", output);
                    StringAssert.Contains($"'Statement-to-be-deleted' in '{k_TestDirectory}/project-statements.ac'", output);
                    StringAssert.DoesNotContain($"Will create:{Environment.NewLine}", output);
                    StringAssert.DoesNotContain($"'statement-1' in '{k_TestDirectory}/project-statements.ac'", output);
                    foreach (var file in m_FetchedTestCases)
                    {
                        StringAssert.Contains(file.ConfigFileName, output);
                    }
                }).AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchValidConfigFromDirectorySucceedWithDryRunAndReconcile()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await CreateDeployTestFilesAsync(m_FetchedTestCases, m_FetchedContents);

        await GetLoggedInCli()
            .Command($"fetch --services access {k_TestDirectory} --dry-run --reconcile")
            .AssertStandardOutput(
                output =>
                {
                    Console.WriteLine(output);
                    StringAssert.Contains($"This is a Dry Run. The result below is the expected result for this operation.{Environment.NewLine}", output);
                    StringAssert.Contains($"Will delete:{Environment.NewLine}", output);
                    StringAssert.Contains($"'Statement-to-be-deleted' in '{k_TestDirectory}/project-statements.ac'", output);
                    StringAssert.Contains($"Will create:{Environment.NewLine}", output);
                    StringAssert.Contains($"'statement-1' in '{k_TestDirectory}/project-statements.ac'", output);
                    foreach (var file in m_FetchedTestCases)
                    {
                        StringAssert.Contains(file.ConfigFileName, output);
                    }
                }).AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task FetchNoConfigsFromDirectorySucceed()
    {
        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        await GetLoggedInCli()
            .Command($"fetch --services access {k_TestDirectory}")
            .AssertStandardOutputContains("No content fetched")
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
