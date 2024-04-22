using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.MockServer.Common;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.CloudContentDeliveryTests;

[TestFixture]
public class CloudContentDeliveryBadgeTests : UgsCliFixture
{

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

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
    public async Task CloudContentDeliveryBadgeBadCommand()
    {
        await GetLoggedInCli()
            .Command("ccd badges badcommand")
            .AssertStandardErrorContains("Unrecognized command or argument 'badcommand'.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBadgeBadOption()
    {
        await GetLoggedInCli()
            .Command("ccd badges list --badOption")
            .AssertStandardErrorContains("Unrecognized command or argument '--badOption'.")
            .AssertExitCode(ExitCode.HandledError)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBadgeList()
    {
        await GetLoggedInCli()
            .Command("ccd badges list")
            .AssertStandardOutputContains(
                "name: badge 1")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBadgeListInJson()
    {
        await GetLoggedInCli()
            .Command("ccd badges list --json")
            .AssertStandardOutputContains("Name\": \"badge 1\"")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBadgeListWithFiltersAndParameters()
    {
        await GetLoggedInCli()
            .Command("ccd badges list -pa 1 -pp 10 -n badge -s name -o asc")
            .AssertStandardOutputContains(
                "name: badge 1")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBadgeCreate()
    {
        await GetLoggedInCli()
            .Command("ccd badges create 1 mybadge")
            .AssertNoErrors()
            .AssertStandardOutputContains(
                "name: badge 1")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

    [Test]
    public async Task CloudContentDeliveryBadgeDelete()
    {
        await GetLoggedInCli()
            .Command("ccd badges delete mybadge")
            .AssertStandardOutputContains("Deleting badge...")
            .AssertExitCode(ExitCode.Success)
            .ExecuteAsync();
    }

}
