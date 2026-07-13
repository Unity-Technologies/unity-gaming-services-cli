using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.MockServer;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.ObservabilityTests;

public class ObservabilityTests : UgsCliFixture
{
    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        SetupProjectAndEnvironment();

        MockApi.Server?.ResetMappings();
        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new ObservabilityApiMock());
    }

    [Test]
    public async Task LogsListSucceed()
    {
        await AssertSuccess("observability logs list", expectedResult: "Hello, world!");
    }

    [Test]
    public async Task LogsListSucceedWithFilters()
    {
        await AssertSuccess(
            "observability logs list --from now-3h --to now --query severityText=Error --offset 0 --limit 50",
            expectedResult: "Hello, world!");
    }

    [Test]
    public async Task LogsListSucceedJson()
    {
        await AssertSuccess("observability logs list --json", expectedResult: "\"Total\": 2");
    }

    [Test]
    public async Task LogsListUsingAliasSucceed()
    {
        await AssertSuccess("obs logs list", expectedResult: "Goodbye, world!");
    }

    async Task AssertSuccess(string command, string? expectedMessage = null, string? expectedResult = null)
    {
        var test = GetLoggedInCli()
            .Command(command);
        if (expectedMessage != null)
        {
            test = test.AssertStandardErrorContains(expectedMessage.ReplaceLineEndings());
        }

        if (expectedResult != null)
        {
            test = test.AssertStandardOutputContains(expectedResult.ReplaceLineEndings());
        }
        await test.ExecuteAsync();
    }
}
