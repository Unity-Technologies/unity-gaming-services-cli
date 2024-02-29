using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.Economy;

public class EconomyDeployTests : DeployTestsFixture
{
    static CurrencyItemResponse s_Currency = new(
        "SILVER_TOKEN",
        "Silver Token",
        CurrencyItemResponse.TypeEnum.CURRENCY,
        0,
        100,
        "custom data",
        new ModifiedMetadata(new DateTime(2023, 1, 1)),
        new ModifiedMetadata(new DateTime(2023, 1, 1))
    );
    static string s_ResourceFileText = s_Currency.ToJson();

    protected override AuthoringTestCase GetValidTestCase()
    {
        return new AuthoringTestCase(
            s_ResourceFileText,
            s_Currency.Name,
            $"{EconomyApiMock.ValidFileName}.ecc",
            "Currency",
            100,
            Statuses.Deployed,
            "",
            TestDirectory);
    }

    protected override AuthoringTestCase GetInvalidTestCase()
    {
        return new AuthoringTestCase(
            "bad file content",
            s_Currency.Name,
            $"{EconomyApiMock.ValidFileName}.ecc",
            "Currency",
            100,
            Statuses.FailedToRead,
            "",
            TestDirectory);
    }

    [SetUp]
    public new async Task SetUp()
    {
        await MockApi.MockServiceAsync(new EconomyApiMock());
        await MockApi.MockServiceAsync(new LeaderboardApiMock());
        await MockApi.MockServiceAsync(new TriggersApiMock());
    }

    [Test]
    public async Task DeployWithReconcileWillDeleteRemoteFiles()
    {
        var content =
            new DeployContent(
                EconomyApiMock.Currency.Name,
                "Currency",
                "Remote",
                100.0f,
                Statuses.Deployed,
                "Deleted remotely",
                SeverityLevel.Success);
        var createdContentList = await CreateDeployTestFilesAsync(DeployedTestCases);

        //TODO: remove this after message details will be adjusted in other services or removed from Economy module
        createdContentList[0].Status = new DeploymentStatus(Statuses.Deployed, "Created remotely", SeverityLevel.Success);
        // deployed content list has the same as the created + the content economy content deployed
        var deployedContentList = createdContentList.ToList();
        deployedContentList.Add(content);

        var logResult = CreateResult(
            createdContentList,
            Array.Empty<DeployContent>(),
            new[]
            {
                content
            },
            deployedContentList,
            Array.Empty<DeployContent>());

        var resultString = JsonConvert.SerializeObject(logResult.ToTable("Economy"), Formatting.Indented);

        await GetFullySetCli()
            .Command($"deploy {TestDirectory} -j --reconcile -s economy")
            .AssertStandardOutputContains(resultString)
            .AssertNoErrors()
            .ExecuteAsync();
    }

    [Test]
    public async Task DeployEmptyFolderWithReconcileFails()
    {
        var logResult = CreateResult(
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>(),
            Array.Empty<DeployContent>());

        var messages = new List<JsonLogMessage>();
        messages.Add(
            new JsonLogMessage()
            {
                Message =
                    "Economy service deployment cannot be used in an empty folder while using reconcile option. " +
                    "You cannot have an empty published configuration.",
                Type = "Warning"
            });

        var resultString = JsonConvert.SerializeObject(logResult.ToTable(), Formatting.Indented);
        var messageString = JsonConvert.SerializeObject(messages, Formatting.Indented);

        await GetFullySetCli()
            .Command($"deploy {TestDirectory} -j --reconcile -s economy")
            .AssertStandardOutputContains(resultString)
            .AssertStandardErrorContains(messageString)
            .ExecuteAsync();
    }

    class JsonLogMessage
    {
        public string? Message { get; set; }
        public string? Type { get; set; }
    }
}
