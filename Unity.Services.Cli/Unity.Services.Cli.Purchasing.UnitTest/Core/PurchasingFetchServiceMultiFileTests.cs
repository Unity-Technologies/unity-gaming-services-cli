using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using Unity.Services.Cli.Purchasing.Authoring;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

[TestFixture]
class PurchasingFetchServiceMultiFileTests : PurchasingDeployFetchTestBase
{
    PurchasingFetchService? m_Service;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        m_FakeUcatCatalogLoader.ReadCatalogImpl =
            (path, _) => Task.FromResult(MakeEntry(path));
        m_Service = new PurchasingFetchService(m_FakeClient, m_FakeUcatCatalogLoader, m_FakeCsvCatalogLoader);
    }

    [Test]
    public async Task FetchAsync_MultipleLocalEntries_AllUpdated()
    {
        var stems = new[] { "sku-1", "sku-2", "sku-3" };
        m_FakeClient.RemoteItems = stems.Select(s => new CatalogItem
        {
            uSku = s,
            CatalogListingId = CatalogItem.CatalogListingIdPrefix + s,
            ProductType = ProductType.NonConsumable,
        }).ToList();

        var files = stems
            .Select(s => new AuthoringFile(s + Constants.FileExtension))
            .ToArray();

        await m_Service!.FetchAsync(
            MakeFetchInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task FetchAsync_Reconcile_MultipleNewRemoteFiles_AllCreated()
    {
        var stems = new[] { "remote-a", "remote-b", "remote-c" };
        m_FakeClient.RemoteItems = stems.Select(s => new CatalogItem
        {
            uSku = s,
            CatalogListingId = CatalogItem.CatalogListingIdPrefix + s,
            ProductType = ProductType.NonConsumable,
        }).ToList();

        await m_Service!.FetchAsync(
            MakeFetchInput(path: "dir", reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task FetchAsync_Reconcile_MixedDeleteAndCreate()
    {
        var localStems = new[] { "local-only-1", "local-only-2" };
        var remoteStems = new[] { "remote-only-1", "remote-only-2" };

        m_FakeClient.RemoteItems = remoteStems.Select(s => new CatalogItem
        {
            uSku = s,
            CatalogListingId = CatalogItem.CatalogListingIdPrefix + s,
        }).ToList();

        var localFiles = localStems
            .Select(s => new AuthoringFile(s + Constants.FileExtension))
            .ToArray();

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(path: "dir", reconcile: true),
            localFiles,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.DeleteCatalogCalls.Count, Is.EqualTo(2));
        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls.Count, Is.EqualTo(2));
        Assert.That(result.Deleted.Count, Is.EqualTo(2));
        Assert.That(result.Created.Count, Is.EqualTo(2));
    }
}
