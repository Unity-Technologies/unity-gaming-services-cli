using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.CloudContentDeliveryTests;

[TestFixture]
public class CloudContentDeliveryEntryTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir");
    static readonly string k_TempDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir/temp");
    static readonly string k_ImageDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir/images");

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        if (!Directory.Exists(k_TestDirectory))
            Directory.CreateDirectory(k_TestDirectory);

        if (!Directory.Exists(k_TempDirectory))
            Directory.CreateDirectory(k_TempDirectory);

        if (!Directory.Exists(k_ImageDirectory))
            Directory.CreateDirectory(k_ImageDirectory);

        await File.WriteAllTextAsync(Path.Join(k_TempDirectory, "foo.txt"), "{}");
        await File.WriteAllTextAsync(Path.Join(k_ImageDirectory, "image.jpg"), "{}");

        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new CloudContentDeliveryApiMock());

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);
        SetConfigValue("bucket-name", CommonKeys.ValidBucketName);
    }

    [TearDown]
    public void TearDown()
    {
        MockApi.Server?.ResetMappings();
    }

    [Test]
    public async Task CloudContentDeliveryListEntries()
    {
        await GetLoggedInCli()
            .Command("ccd entries list")
            .AssertStandardOutputContains("id: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryListEntriesWithParams()
    {
        await GetLoggedInCli()
            .Command(
                "ccd entries list --page=1 --per-page=10 --label=abc --sort-by=path --sort-order=desc")
            .AssertStandardOutputContains("id: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryListEntriesWithParamsInJson()
    {
        await GetLoggedInCli()
            .Command(
                "ccd entries list --page=1 --per-page=10 --label=def --sort-by=content_size --sort-order=asc --json")
            .AssertStandardOutputContains(
                "\"Id\": \"00000000-0000-0000-0000-000000000000\"")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryInfoEntries()
    {
        await GetLoggedInCli()
            .DebugCommand("ccd entries info myentry")
            .AssertStandardOutputContains("entryid: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryUpdateEntries()
    {
        await GetLoggedInCli()
            .Command("ccd entries update myentry -l mylabel")
            .AssertStandardOutputContains(
                "- my label")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryDeleteEntries()
    {
        await GetLoggedInCli()
            .Command("ccd entries delete myentry")
            .AssertStandardOutputContains("Deleting entry...")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliverySyncEntriesWithParams()
    {
        await GetLoggedInCli()
            .DebugCommand(
                $"ccd entries sync {k_TestDirectory} -r -u mybadge")
            .AssertStandardOutputContains("operationCompletedSuccessfully: true")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliverySyncEntriesWithParamsIncludeSyncEntriesOnlyFalse()
    {
        await GetLoggedInCli()
            .DebugCommand(
                $"ccd entries sync {k_TestDirectory} -r -u mybadge --include-entries-added-during-sync")
            .AssertStandardOutputContains("operationCompletedSuccessfully: true")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliverySyncEntriesWithBadgeButNoRelease()
    {
        await GetLoggedInCli()
            .DebugCommand(
                $"ccd entries sync {k_TestDirectory} -u mybadge")
            .AssertStandardErrorContains("The badge option requires the 'create release' option to be set to true.")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliverySyncEntriesWithReleaseNoteButNoRelease()
    {
        await GetLoggedInCli()
            .DebugCommand(
                $"ccd entries sync {k_TestDirectory} -n mynotes")
            .AssertStandardErrorContains("The release notes option requires the 'create release' option to be set to true. As a result, no release notes were added.")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryDownloadEntries()
    {
        await GetLoggedInCli()
            .Command(
                $"ccd entries download myentry")
            .AssertStandardOutputContains("Downloading entry content...")
            .AssertNoErrors()
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryCopyEntries()
    {
        await GetLoggedInCli()
            .Command(
                $"ccd entries copy {k_TempDirectory}/foo.txt foo.txt")
            .AssertStandardOutputContains(
                "entryid: 00000000-0000-0000-0000-000000000000")
            .AssertNoErrors()
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryCopyEntriesMissingArgument()
    {
        await GetLoggedInCli()
            .Command(
                $"ccd entries copy {k_TestDirectory}/foo")
            .AssertStandardErrorContains("Required argument missing for command: 'copy'.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

}
