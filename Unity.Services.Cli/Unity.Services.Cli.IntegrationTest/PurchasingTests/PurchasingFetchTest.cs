using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.IntegrationTest.PurchasingTests;

public class PurchasingFetchTest : PurchasingBaseFixture
{
    [Test]
    public async Task Fetch_EmptyDirectory()
    {
        await GetLoggedInCli()
            .Command($"fetch {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("\"Result\": []")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Fetch_IdenticalLocalFile_SkipsUpdate()
    {
        await File.WriteAllTextAsync(TestItemPath, k_TestItemJson);

        await GetLoggedInCli()
            .Command($"fetch {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("\"Result\": []")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Fetch_DifferentLocalFile_Updates()
    {
        var differentItem = k_TestItemJson.Replace("4.99", "9.99");
        await File.WriteAllTextAsync(TestItemPath, differentItem);

        await GetLoggedInCli()
            .Command($"fetch {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("test-item.ucat")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Fetch_DryRun()
    {
        var differentItem = k_TestItemJson.Replace("4.99", "9.99");
        await File.WriteAllTextAsync(TestItemPath, differentItem);

        await GetLoggedInCli()
            .Command($"fetch {TestDirectory} --services purchasing --dry-run -j")
            .AssertStandardOutputContains("test-item.ucat")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Fetch_Reconcile_CreatesLocalFile()
    {
        await GetLoggedInCli()
            .Command($"fetch {TestDirectory} --services purchasing --reconcile -j")
            .AssertStandardOutputContains("test-item.ucat")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Fetch_InvalidFile()
    {
        await File.WriteAllTextAsync(
            Path.Combine(TestDirectory, "invalid.ucat"),
            "not valid json {{{");

        await GetLoggedInCli()
            .Command($"fetch {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("invalid.ucat")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }
}
