using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.MockServer.ServiceMocks;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Fetch;

public class EconomyFetchTests : FetchTestsFixture
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

    readonly List<DeployContent> m_FetchedContents = new();

    protected override AuthoringTestCase GetLocalTestCase()
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

    protected override AuthoringTestCase GetRemoteTestCase()
    {
        return new AuthoringTestCase(
            EconomyApiMock.Currency.ToJson(),
            EconomyApiMock.Currency.Name,
            $"{EconomyApiMock.Currency.Id}.ecc",
            "Currency",
            100,
            Statuses.Deployed,
            "",
            TestDirectory);
    }

    [SetUp]
    public new async Task SetUp()
    {
        m_FetchedContents.Clear();
        await MockApi
            .MockServiceAsync(new EconomyApiMock());
    }

    [TestCase("", "")]
    [TestCase("", "--json")]
    // TODO: Remove this local test after EDX-2319 is done
    public override async Task FetchEmptyDirectorySuccessfully_FetchAndCreate_WithReconcile(string dryRunOption, string jsonOption)
    {
        await base.FetchEmptyDirectorySuccessfully_FetchAndCreate_WithReconcile(dryRunOption, jsonOption);
    }
}
