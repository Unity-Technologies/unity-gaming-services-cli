using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using Unity.Services.Cli.Purchasing.Authoring;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

[TestFixture]
class PurchasingFetchHandlerTests : PurchasingDeployFetchTestBase
{
    const string k_Stem = "sku-a";
    const string k_Path = k_Stem + Constants.FileExtension;
    const string k_ListingId = "catalog/" + k_Stem;

    PurchasingFetchService? m_Service;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        m_FakeUcatCatalogLoader.ReadCatalogImpl =
            (path, _) => Task.FromResult(MakeEntry(path));
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = k_Stem, CatalogListingId = k_ListingId, ProductType = ProductType.NonConsumable },
        };
        m_Service = new PurchasingFetchService(m_FakeClient, m_FakeUcatCatalogLoader, m_FakeCsvCatalogLoader);
    }

    [Test]
    public async Task FetchAsync_SingleEntry_CallsList()
    {
        await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeClient.RemoteItems, Is.Not.Null);
    }

    [Test]
    public async Task FetchAsync_SingleEntry_UpdatesEntry()
    {
        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Fetched.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_DryRun_DoesNotWrite()
    {
        await m_Service!.FetchAsync(
            MakeFetchInput(dryRun: true),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls, Is.Empty);
    }
}
