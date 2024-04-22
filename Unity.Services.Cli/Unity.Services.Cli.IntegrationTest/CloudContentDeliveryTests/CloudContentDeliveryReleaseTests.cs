using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.CloudContentDeliveryTests;

[TestFixture]
public class CloudContentDeliveryReleaseTests : UgsCliFixture
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
        SetConfigValue("bucket-name", CommonKeys.ValidBucketName);

    }

    [TearDown]
    public void TearDown()
    {
        MockApi.Server?.ResetMappings();
    }

    [Test]
    public async Task CloudContentDeliveryCreateRelease()
    {
        await GetLoggedInCli()
            .Command("ccd releases create")
            .AssertStandardOutputContains(
                "releaseid: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryUpdateRelease()
    {
        await GetLoggedInCli()
            .Command(
                "ccd releases update 1 --notes=\"my note\"")
            .AssertStandardOutputContains(
                "notes: my note")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryUpdateReleaseMissingRequiredOption()
    {
        await GetLoggedInCli()
            .Command(
                "ccd releases update 1")
            .AssertStandardErrorContains("Option '-n' is required.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryListReleases()
    {
        await GetLoggedInCli()
            .Command("ccd releases list")
            .AssertStandardOutputContains("releaseId: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryListReleasesWithParams()
    {
        await GetLoggedInCli()
            .Command(
                "ccd releases list --page=1 --per-page=10 --release-number=1 --notes=abc --badges=abc --sort-by=created --sort-order=desc")
            .AssertStandardOutputContains("releaseId: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryGetReleaseInfo()
    {
        await GetLoggedInCli()
            .Command("ccd releases info 1")
            .AssertStandardOutputContains(
                "releasenum: 1")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryPromoteBucket()
    {
        await GetLoggedInCli()
            .Command(
                "ccd releases promote 1 ios production")
            .AssertStandardOutputContains("promotionId: 00000000-0000-0000-0000-000000000000")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryGetPromotionStatus()
    {
        await GetLoggedInCli()
            .Command(
                "ccd releases promotions status 00000000-0000-0000-0000-000000000000")
            .AssertStandardOutputContains(
                "promotionStatus: Complete")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

}
