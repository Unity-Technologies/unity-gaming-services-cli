using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.IntegrationTest.PurchasingTests;

public class PurchasingDeployTest : PurchasingBaseFixture
{
    [Test]
    public async Task Deploy_EmptyDirectory()
    {
        await GetLoggedInCli()
            .Command($"deploy {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("\"Result\": []")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Deploy_ValidFile()
    {
        await File.WriteAllTextAsync(TestItemPath, k_TestItemJson);

        await GetLoggedInCli()
            .Command($"deploy {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("test-item.ucat")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Deploy_DryRun()
    {
        await File.WriteAllTextAsync(TestItemPath, k_TestItemJson);

        await GetLoggedInCli()
            .Command($"deploy {TestDirectory} --services purchasing --dry-run -j")
            .AssertStandardOutputContains("test-item.ucat")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Deploy_InvalidFile()
    {
        await File.WriteAllTextAsync(
            Path.Combine(TestDirectory, "invalid.ucat"),
            "not valid json {{{");

        await GetLoggedInCli()
            .Command($"deploy {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("invalid.ucat")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task Deploy_ValidCsvFile_Succeeds()
    {
        await File.WriteAllTextAsync(TestCsvPath, k_TestCsvContent);

        await GetLoggedInCli()
            .Command($"deploy {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("test-item")
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task Deploy_InvalidCsvFile_ReturnsHandledError()
    {
        await File.WriteAllTextAsync(
            Path.Combine(TestDirectory, "invalid.catalog.csv"),
            "Sku,Title,Description,Language,ProductType,CurrencyCode,Amount\n" +
            ",,Missing Sku,en-US,Consumable,USD,0.99\n");

        await GetLoggedInCli()
            .Command($"deploy {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("invalid.catalog.csv")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task Deploy_EmptyCsvFile_ReturnsEmptyResult()
    {
        await File.WriteAllTextAsync(
            Path.Combine(TestDirectory, "empty.catalog.csv"),
            "");

        await GetLoggedInCli()
            .Command($"deploy {TestDirectory} --services purchasing -j")
            .AssertStandardOutputContains("\"Result\": []")
            .AssertNoErrors()
            .ExecuteAsync();
    }
}
