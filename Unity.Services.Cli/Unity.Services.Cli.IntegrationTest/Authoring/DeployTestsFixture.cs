using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Model.TableOutput;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.IntegrationTest.Authoring;

/// <summary>
/// A test fixture to facilitate integration testing
/// </summary>
[TestFixture]
public abstract class DeployTestsFixture : UgsCliFixture
{
    protected static readonly string TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");

    protected readonly List<AuthoringTestCase> DeployedTestCases = new();
    readonly List<AuthoringTestCase> m_DryRunDeployedTestCases = new();

    protected abstract AuthoringTestCase GetValidTestCase();
    protected abstract AuthoringTestCase GetInvalidTestCase();

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }

        Directory.CreateDirectory(TestDirectory);

        DeployedTestCases.Clear();
        m_DryRunDeployedTestCases.Clear();

        MockApi.Server?.ResetMappings();

        await MockApi.MockServiceAsync(new IdentityV1Mock());

        DeployedTestCases.Add(GetDeployedTestCase());
        m_DryRunDeployedTestCases.Add(GetLoadedTestCase());
    }

    AuthoringTestCase GetDeployedTestCase()
    {
        var testCase = GetValidTestCase();
        var type = testCase.DeployedContent.Type;
        testCase.DeployedContent =
            new DeployContent(
                testCase.ConfigFileName, type, testCase.ConfigFilePath, 100, Statuses.Deployed, "");
        return testCase;
    }

    AuthoringTestCase GetLoadedTestCase()
    {
        var testCase = GetValidTestCase();
        var type = testCase.DeployedContent.Type;
        testCase.DeployedContent =
            new DeployContent(
                testCase.ConfigFileName, type, testCase.ConfigFilePath, 0, Statuses.Loaded, "");
        return testCase;
    }

    [TearDown]
    public void TearDown()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }
    }

    #region base behavior

    [Test]
    public virtual async Task DeployFromEmptyDirectorySucceed()
    {
        await GetFullySetCli()
            .Command($"deploy {TestDirectory}")
            .AssertStandardOutputContains($"No content deployed")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public virtual async Task DeployValidConfigFromDirectorySucceed()
    {
        var deployedContentList =
            await CreateDeployTestFilesAsync(DeployedTestCases);
        var deployedConfigFileString = $"{Environment.NewLine}    {deployedContentList[0]}";

        await GetFullySetCli()
            .Command($"deploy {TestDirectory}")
            .AssertStandardOutputContains($"Successfully deployed the following files:{deployedConfigFileString}")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public virtual async Task DeployInvalidConfigFromDirectoryFails()
    {
        var invalidTestCase = GetInvalidTestCase();
        await CreateDeployTestFilesAsync(
            new List<AuthoringTestCase>()
            {
                invalidTestCase
            });
        var deployedConfigFileString =
            $"'{invalidTestCase.ConfigFilePath}' - Status: {Statuses.FailedToRead}";

        await GetFullySetCli()
            .Command($"deploy {TestDirectory}")
            .AssertStandardOutputContains($"Failed to deploy:{Environment.NewLine}    {deployedConfigFileString}")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public virtual async Task DeployValidConfigFromDirectorySucceedWithJsonOutput()
    {
        var deployedContents = await CreateDeployTestFilesAsync(DeployedTestCases);

        deployedContents[0].Status = new DeploymentStatus(Statuses.Deployed, "Created remotely");

        var logResult = CreateResult(
            deployedContents,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            deployedContents,
            Array.Empty<DeployContent>());

        var resultString = JsonConvert.SerializeObject(logResult.ToTable(), Formatting.Indented);
        await GetFullySetCli()
            .Command($"deploy {TestDirectory} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    #endregion

    #region dry run

    [Test]
    public virtual async Task DeployConfig_DryRun()
    {
        var contentList = await CreateDeployTestFilesAsync(m_DryRunDeployedTestCases);

        contentList[0].Status = new DeploymentStatus(Statuses.Deployed, "Created remotely", SeverityLevel.Success);

        var logResult = new DeploymentResult(
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            contentList,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            true);
        var resultString = JsonConvert.SerializeObject(logResult.ToTable(), Formatting.Indented);
        await GetFullySetCli()
            .Command($"deploy {TestDirectory} -j --dry-run")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    #endregion

    #region reconcile

    [Test]
    public virtual async Task DeployWithoutReconcileWillNotDeleteRemoteFiles()
    {
        var contentList = await CreateDeployTestFilesAsync(DeployedTestCases);

        contentList[0].Status = new DeploymentStatus(Statuses.Deployed, "Created remotely", SeverityLevel.Success);

        var logResult = CreateResult(
            contentList,
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            contentList,
            Array.Empty<DeployContent>());
        var resultString = JsonConvert.SerializeObject(logResult.ToTable(), Formatting.Indented);

        await GetFullySetCli()
            .Command($"deploy {TestDirectory} -j")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    #endregion

    protected async Task<List<DeployContent>> CreateDeployTestFilesAsync(
        IReadOnlyList<AuthoringTestCase> testCases)
    {
        List<DeployContent> deployedContentList = new();
        foreach (var testCase in testCases)
        {
            await File.WriteAllTextAsync(testCase.ConfigFilePath, testCase.ConfigValue);

            deployedContentList.Add(testCase.DeployedContent);
        }

        return deployedContentList;
    }

    public static DeploymentResult CreateResult(
        IReadOnlyList<DeployContent> created,
        IReadOnlyList<DeployContent> updated,
        IReadOnlyList<DeployContent> deleted,
        IReadOnlyList<DeployContent> deployed,
        IReadOnlyList<DeployContent> failed,
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
                status));
        }

        return new DeploymentResult(updated, deleted, createdCopy, deployed, failed, dryRun);
    }

    public static TableContent CreateTableResult(
        IReadOnlyList<DeployContent> created,
        IReadOnlyList<DeployContent> updated,
        IReadOnlyList<DeployContent> deleted,
        IReadOnlyList<DeployContent> deployed,
        IReadOnlyList<DeployContent> failed,
        bool dryRun = false)
    {
        var tableResult = new TableContent();

        foreach (var item in deployed)
        {
            tableResult.AddRow(RowContent.ToRow(item));

            var updatedRows = updated.Where(i => i.Path == item.Path).Select(RowContent.ToRow).ToList();
            var createdRows = created.Where(i => i.Path == item.Path).Select(RowContent.ToRow).ToList();

            tableResult.AddRows(updatedRows);
            tableResult.AddRows(createdRows);

        }

        var deletedRows = deleted.Select(RowContent.ToRow).ToList();
        var failedRows = failed.Select(RowContent.ToRow).ToList();

        tableResult.AddRows(deletedRows);
        tableResult.AddRows(failedRows);

        return tableResult;
    }
}
