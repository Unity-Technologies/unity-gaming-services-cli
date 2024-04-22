using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.CloudContentDeliveryTests;

[TestFixture]
public class CloudContentDeliveryBucketTests : UgsCliFixture
{
    static readonly string k_TestDirectory = Path.Combine(UgsCliBuilder.RootDirectory, ".tmp/FilesDir");

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        if (!Directory.Exists(k_TestDirectory))
            Directory.CreateDirectory(k_TestDirectory);

        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new CloudContentDeliveryApiMock());

        SetConfigValue("project-id", CommonKeys.ValidProjectId);
        SetConfigValue("environment-name", CommonKeys.ValidEnvironmentName);

    }

    [TearDown]
    public void TearDown()
    {
        MockApi.Server?.ResetMappings();
    }

    [Test]
    public async Task CloudContentDeliveryBucketsListReturnsJsonResponse()
    {
        await GetLoggedInCli()
            .Command("ccd buckets list --json")
            .AssertStandardOutputContains(
                "\"Id\": \"00000000-0000-0000-0000-000000000000\"")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBucketsList()
    {
        await GetLoggedInCli()
            .Command("ccd buckets list")
            .AssertStandardOutputContains(
                "id: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBucketsListWithFiltersAndParameters()
    {
        var filterName = "TestBucket";
        var page = 1;
        var perPage = 10;
        var sortBy = "name";
        var sortOrder = "asc";
        await GetLoggedInCli()
            .Command(
                $"ccd buckets list --filter-by-name {filterName} --page {page} --per-page {perPage} --sort-by {sortBy} --sort-order {sortOrder}")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();

    }

    [Test]
    public async Task CloudContentDeliveryBucketsCreate()
    {
        var bucketName = "TestBucket";
        var bucketDescription = "Test bucket description";
        await GetLoggedInCli()
            .Command($"ccd buckets create {bucketName} --description \"{bucketDescription}\" --private")
            .AssertNoErrors()
            .AssertStandardOutputContains("id: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();

    }

    [Test]
    public async Task CloudContentDeliveryBucketsInfo()
    {

        await GetLoggedInCli()
            .Command($"ccd buckets info ios")
            .AssertNoErrors()
            .AssertStandardOutputContains(
                "id: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBucketsDelete()
    {

        await GetLoggedInCli()
            .Command($"ccd buckets delete ios")
            .AssertNoErrors()
            .AssertStandardOutputContains("Bucket deleted.")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();

    }

    [Test]
    public async Task CloudContentDeliveryBadCommand()
    {
        await GetLoggedInCli()
            .Command("ccd bad command")
            .AssertStandardOutputContains("'bad' was not matched.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryPromoteOnlyTrue()
    {
        await GetLoggedInCli()
            .Command("ccd buckets permissions update ios --action=Write -m Allow -r User")
            .AssertStandardOutputContains("action: write")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryMissingRequiredPromoteOnlyOption()
    {
        await GetLoggedInCli()
            .Command("ccd buckets permissions update ios")
            .AssertStandardErrorContains("Option '-a' is required.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryPromoteOnlyFalse()
    {
        await GetLoggedInCli()
            .Command("ccd buckets permissions update ios --action=Write -m Deny -r User")
            .AssertStandardOutputContains("action: write")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

}
