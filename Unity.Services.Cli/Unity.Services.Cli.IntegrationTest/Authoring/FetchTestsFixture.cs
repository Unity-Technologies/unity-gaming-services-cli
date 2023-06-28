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

namespace Unity.Services.Cli.IntegrationTest.Authoring;

/// <summary>
/// A test fixture to facilitate integration testing
/// </summary>
[TestFixture]
public abstract class FetchTestsFixture : UgsCliFixture
{
    protected static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "FilesDir");
    protected List<AuthoringTestCase> m_FetchedTestCases = new();

    protected abstract AuthoringTestCase GetLocalTestCase();
    protected abstract AuthoringTestCase GetRemoteTestCase();

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(k_TestDirectory))
        {
            Directory.Delete(k_TestDirectory, true);
        }

        Directory.CreateDirectory(k_TestDirectory);

        m_FetchedTestCases.Clear();

        m_MockApi.Server?.ResetMappings();

        await m_MockApi.MockServiceAsync(new IdentityV1Mock());
    }

    public static AuthoringTestCase SetTestCase(AuthoringTestCase testCase, string status)
    {
        var type = testCase.DeployedContent.Type;
        testCase.DeployedContent =
            new DeployContent(
                testCase.ConfigFileName, type, testCase.ConfigFilePath, 100, status, "");
        return testCase;
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

    // General Fetch Behavior
    [Test]
    public virtual async Task FetchInvalidPath()
    {
        var invalidDirectory = Path.GetFullPath("invalid-directory");
        var expectedOutput = $"Path \"{invalidDirectory}\" could not be found.";

        await GetFullySetCli()
            .Command($"fetch {invalidDirectory}")
            .AssertStandardErrorContains(expectedOutput)
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    // General Fetch Behavior
    [TestCase("", "")]
    [TestCase("", "--json")]
    [TestCase("--dry-run", "--json")]
    [TestCase("--dry-run", "")]
    public virtual async Task FetchNoConfigFromDirectorySucceed(string dryRunOption, string jsonOption)
    {
        var resultString = FormatOutput(new List<DeployContent>(), new List<string>()
        {
            dryRunOption,
            jsonOption
        });
        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} {dryRunOption} {jsonOption}")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [TestCase("")]
    [TestCase("--dry-run")]
    public virtual async Task FetchValidConfigFromDirectoryWithOptionSucceed(string dryRunOption)
    {
        m_FetchedTestCases.Add(SetTestCase(GetLocalTestCase(), Statuses.Deleted));
        var fetchedContent = await CreateFetchTestFilesAsync(m_FetchedTestCases);
        var resultString = FormatDefaultOutput(fetchedContent, !string.IsNullOrEmpty(dryRunOption));
        await GetLoggedInCli()
            .Command($"fetch {k_TestDirectory} -p {CommonKeys.ValidProjectId} -e {CommonKeys.ValidEnvironmentName} {dryRunOption}")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [TestCase("", "")]
    [TestCase("--dry-run", "")]
    [TestCase("", "--json")]
    [TestCase("--dry-run", "--json")]
    public virtual async Task FetchValidConfigFromDirectorySuccessfully_FetchAndDelete(string dryRunOption, string jsonOption)
    {
        //Case: Fetch files successfully but no existing file in service so deletes local files
        m_FetchedTestCases.Add(SetTestCase(GetLocalTestCase(), Statuses.Deleted));
        var fetchedContent = await CreateFetchTestFilesAsync(m_FetchedTestCases);

        var resultString = FormatOutput(fetchedContent, new List<string>()
        {
            dryRunOption,
            jsonOption
        });

        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} {dryRunOption} {jsonOption}")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [TestCase("", "")]
    [TestCase("--dry-run", "")]
    [TestCase("", "--json")]
    [TestCase("--dry-run", "--json")]
    public virtual async Task FetchValidConfigFromDirectorySuccessfully_FetchAndUpdate(string dryRunOption, string jsonOption)
    {
        //Case: Fetch files successfully and updates local files
        m_FetchedTestCases.Add(SetTestCase(GetRemoteTestCase(), Statuses.Updated));
        var fetchedContent = await CreateFetchTestFilesAsync(m_FetchedTestCases);
        var resultString = FormatOutput(fetchedContent, new List<string>()
        {
            dryRunOption,
            jsonOption
        });

        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} {dryRunOption} {jsonOption}")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [TestCase("", "")]
    [TestCase("--dry-run", "")]
    [TestCase("", "--json")]
    [TestCase("--dry-run", "--json")]
    public virtual async Task FetchEmptyDirectorySuccessfully_FetchAndCreate_WithReconcile(string dryRunOption, string jsonOption)
    {
        //Case: Fetch files successfully and create local files
        m_FetchedTestCases.Add(SetTestCase(GetRemoteTestCase(), Statuses.Created));
        List<DeployContent> fetchedContent = new();
        foreach (var testCase in m_FetchedTestCases)
        {
            fetchedContent.Add(testCase.DeployedContent);
        }
        var resultString = FormatOutput(fetchedContent, new List<string>()
        {
            dryRunOption,
            jsonOption
        });
        await GetFullySetCli()
            .Command($"fetch {k_TestDirectory} {dryRunOption} {jsonOption} --reconcile -s economy")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    static protected string FormatOutput(List<DeployContent> deployContentList, List<string> options)
    {
        if (options.Contains("--json") || options.Contains("-j"))
        {
            return FormatJsonOutput(deployContentList, options.Contains("--dry-run"));
        }
        else
        {
            return FormatDefaultOutput(deployContentList, options.Contains("--dry-run"));
        }
    }

    protected static async Task<List<DeployContent>> CreateFetchTestFilesAsync(IReadOnlyList<AuthoringTestCase> testCases)
    {
        List<DeployContent> deployedContentList = new();
        foreach (var testCase in testCases)
        {
            await File.WriteAllTextAsync(testCase.ConfigFilePath, testCase.ConfigValue);
            deployedContentList.Add(testCase.DeployedContent);
        }

        return deployedContentList;
    }

    protected static string FormatDefaultOutput(List<DeployContent> deployContentList, bool isDryRun)
    {
        string output = "";

        var fetchResult = GetFetchResult(deployContentList, isDryRun);

        if (fetchResult.Fetched.Count > 0)
        {
            string fetched = $"Successfully fetched the following files:";
            foreach (var content in fetchResult.Fetched)
            {
                fetched = string.Concat(fetched, $"{Environment.NewLine}    {content}");
            }
            output = string.Concat(output, fetched + $"{Environment.NewLine}");
        }
        else
        {
            output = $"No content fetched{Environment.NewLine}";
        }

        if (fetchResult.Deleted.Count > 0)
        {
            string deleted = $"{Environment.NewLine}Deleted:";
            if (isDryRun)
                deleted = $"{Environment.NewLine}Will delete:";
            foreach (var content in fetchResult.Deleted)
            {
                deleted = string.Concat(deleted, $"{Environment.NewLine}    {content}");
            }
            output = string.Concat(output, deleted);
        }

        if (fetchResult.Updated.Count > 0)
        {
            string updated = $"{Environment.NewLine}Updated:";
            if (isDryRun)
                updated = $"{Environment.NewLine}Will update:";
            foreach (var content in fetchResult.Updated)
            {
                updated = string.Concat(updated, $"{Environment.NewLine}    {content}");
            }
            output = string.Concat(output, updated);
        }

        if (fetchResult.Created.Count > 0)
        {
            string created = $"{Environment.NewLine}Created:";
            if (isDryRun)
                created = $"{Environment.NewLine}Will create:";
            foreach (var content in fetchResult.Created)
            {
                created = string.Concat(created, $"{Environment.NewLine}    {content}");
            }
            output = string.Concat(output, created);
        }

        return output;
    }

    protected static string FormatJsonOutput(List<DeployContent> deployContentList, bool isDryRun)
    {
        var fetchResult = GetFetchResult(deployContentList, isDryRun);
        return JsonConvert.SerializeObject(fetchResult, Formatting.Indented);
    }

    static FetchResult GetFetchResult(List<DeployContent> deployContentList, bool isDryRun)
    {
        var updatedContent = deployContentList
            .Where(t => string.Equals(t.Status.Message, Statuses.Updated))
            .ToArray();

        var deletedContent = deployContentList
            .Where(t => string.Equals(t.Status.Message, Statuses.Deleted))
            .ToArray();

        var createdContent = deployContentList
            .Where(t => string.Equals(t.Status.Message, Statuses.Created))
            .ToArray();

        var fetchedContent = deployContentList
            .Where(t => Math.Abs(t.Progress - 100.0f) < 0.01f)
            .ToArray();

        var failedContent = deployContentList
                .Except(fetchedContent)
                .ToArray();

        return new FetchResult(
            updatedContent,
            deletedContent,
            createdContent,
            isDryRun ?
                Array.Empty<DeployContent>() :
                fetchedContent,
            failedContent,
            isDryRun);
    }
}
