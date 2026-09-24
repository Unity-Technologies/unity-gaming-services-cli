using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using Unity.Services.Cli.Purchasing.Authoring;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

[TestFixture]
class PurchasingFetchServiceTests : PurchasingDeployFetchTestBase
{
    const string k_Stem = "my-item";
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
    public async Task FetchAsync_CallsClientInitialize()
    {
        const string envId = "env-abc";
        const string projId = "proj-xyz";

        await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            projId,
            envId,
            null,
            CancellationToken.None);

        Assert.That(m_FakeClient.LastEnvironmentId, Is.EqualTo(envId));
        Assert.That(m_FakeClient.LastProjectId, Is.EqualTo(projId));
    }

    [Test]
    public async Task FetchAsync_UpdatesLocalEntryFromRemote()
    {
        await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_WhenNoMatchingLocal_NoWrite()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "other-item", CatalogListingId = "catalog/other-item" },
        };

        await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: false),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_UnmatchedLocalItem_NotInFetchedResult()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "other-item", CatalogListingId = "catalog/other-item" },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: false),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Fetched, Is.Empty);
        Assert.That(result.Failed, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_Reconcile_CreatesFileForRemoteOnlyItem()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "remote-only", CatalogListingId = "catalog/remote-only" },
        };

        await m_Service!.FetchAsync(
            MakeFetchInput(path: "dir", reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_Reconcile_DeletesLocalItemNotInRemote()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>();

        await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: true),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.DeleteCatalogCalls.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_OnClientListException_MarksEntriesAsFailed()
    {
        m_FakeClient.ListException = new ClientException("list failed", null);

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task FetchAsync_DryRun_Reconcile_DoesNotCreateOrDelete()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "remote-only", CatalogListingId = "catalog/remote-only" },
        };

        await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: true, dryRun: true),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls, Is.Empty);
        Assert.That(m_FakeUcatCatalogLoader.DeleteCatalogCalls, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_DryRun_Reconcile_ReportsItemsCorrectly()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "remote-only", CatalogListingId = "catalog/remote-only" },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: true, dryRun: true),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Deleted.Count, Is.EqualTo(1));
        Assert.That(result.Created.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_FailedToLoad_IncludedInResult()
    {
        m_FakeUcatCatalogLoader.ReadCatalogImpl =
            (path, _) => Task.FromResult(MakeEntry(path, asError: true));

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_FailedToLoad_StatusNotOverwritten()
    {
        m_FakeUcatCatalogLoader.ReadCatalogImpl =
            (path, _) => Task.FromResult(MakeEntry(path, asError: true));

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Fetched, Is.Empty);
        Assert.That(result.Failed.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_UpdatedEntry_InCorrectResultBucket()
    {
        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Updated.Count, Is.EqualTo(1));
        Assert.That(result.Fetched.Count, Is.EqualTo(1));
        Assert.That(result.Deleted, Is.Empty);
        Assert.That(result.Created, Is.Empty);
        Assert.That(result.Failed, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_IdenticalLocalAndRemote_SkipsUpdate()
    {
        m_FakeUcatCatalogLoader.ReadCatalogImpl = (path, _) =>
        {
            var entry = MakeEntry(path);
            entry.CatalogItem.ProductType = ProductType.NonConsumable;
            return Task.FromResult(entry);
        };

        var remoteItem = new CatalogItem
        {
            uSku = k_Stem,
            CatalogListingId = k_ListingId,
            ProductType = ProductType.NonConsumable,
        };

        m_FakeClient.RemoteItems = new List<CatalogItem> { remoteItem };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Updated, Is.Empty);
        Assert.That(result.Fetched, Is.Empty);
        Assert.That(result.Failed, Is.Empty);
        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_DuplicateLocalFiles_MarkedAsFailed()
    {
        var path1 = k_Stem + "_a" + Constants.FileExtension;
        var path2 = k_Stem + "_b" + Constants.FileExtension;
        m_FakeUcatCatalogLoader.ReadCatalogImpl = (path, _) =>
        {
            var entry = MakeEntry(path);
            entry.CatalogItem.CatalogListingId = k_ListingId;
            return Task.FromResult(entry);
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(path1), new AuthoringFile(path2)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(2));
        Assert.That(result.Fetched, Is.Empty);
        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_Reconcile_DuplicateLocalFiles_DoesNotRecreateFromRemote()
    {
        var path1 = k_Stem + "_a" + Constants.FileExtension;
        var path2 = k_Stem + "_b" + Constants.FileExtension;
        m_FakeUcatCatalogLoader.ReadCatalogImpl = (path, _) =>
        {
            var entry = MakeEntry(path);
            entry.CatalogItem.CatalogListingId = k_ListingId;
            return Task.FromResult(entry);
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: true),
            [new AuthoringFile(path1), new AuthoringFile(path2)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Created, Is.Empty);
        Assert.That(result.Failed.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task FetchAsync_CreateOrUpdateThrows_MarksItemAsFailed()
    {
        m_FakeUcatCatalogLoader.CreateOrUpdateException = new IOException("disk full");

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
        Assert.That(result.Fetched, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_DeleteThrows_MarksItemAsFailed()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>();
        m_FakeUcatCatalogLoader.DeleteCatalogException = new IOException("permission denied");

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: true),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
        Assert.That(result.Fetched, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_Reconcile_CreateThrows_MarksItemAsFailed()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "remote-only", CatalogListingId = "catalog/remote-only" },
        };
        m_FakeUcatCatalogLoader.CreateOrUpdateException = new IOException("disk full");

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(path: "dir", reconcile: true),
            Array.Empty<AuthoringFile>(),
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
        Assert.That(result.Fetched, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_SuccessfulUpdate_SetsProgressTo100()
    {
        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Updated[0].Progress, Is.EqualTo(100f));
    }

    [Test]
    public async Task FetchAsync_CsvFile_UpdatesAndRewritesCsv()
    {
        const string csvPath = "catalog.catalog.csv";
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = k_Stem, CatalogListingId = k_ListingId },
                },
                new List<AssetState>()));

        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = k_Stem, CatalogListingId = k_ListingId, ProductType = ProductType.NonConsumable },
        };

        await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(csvPath)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeCsvCatalogLoader.WriteCatalogPaths, Has.Count.EqualTo(1));
        Assert.That(m_FakeCsvCatalogLoader.WrittenItems[csvPath], Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_CsvFile_DryRun_DoesNotWrite()
    {
        const string csvPath = "catalog.catalog.csv";
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = k_Stem, CatalogListingId = k_ListingId },
                },
                new List<AssetState>()));

        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = k_Stem, CatalogListingId = k_ListingId, ProductType = ProductType.NonConsumable },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(dryRun: true),
            [new AuthoringFile(csvPath)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeCsvCatalogLoader.WriteCatalogPaths, Is.Empty);
        Assert.That(result.Updated, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_CsvFile_Reconcile_DeletesItemFromCsv()
    {
        const string csvPath = "catalog.catalog.csv";
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = "local-only", CatalogListingId = CatalogItem.CatalogListingIdPrefix + "local-only" },
                },
                new List<AssetState>()));

        m_FakeClient.RemoteItems = new List<CatalogItem>();

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: true),
            [new AuthoringFile(csvPath)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Deleted, Has.Count.EqualTo(1));
        Assert.That(m_FakeCsvCatalogLoader.DeletedPaths, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_CsvFile_Reconcile_RemoteOnlyCreatedAsUcat()
    {
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult<(List<CatalogItem>, List<AssetState>)>(
                (new List<CatalogItem>(),
                 new List<AssetState>()));

        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "remote-item", CatalogListingId = CatalogItem.CatalogListingIdPrefix + "remote-item" },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(reconcile: true),
            [new AuthoringFile("catalog.catalog.csv")],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Created, Has.Count.EqualTo(1));
        Assert.That(m_FakeUcatCatalogLoader.CreateOrUpdateCalls, Has.Count.EqualTo(1));
        Assert.That(m_FakeCsvCatalogLoader.WriteCatalogPaths, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_CsvWriteError_MarksItemsAsFailed()
    {
        const string csvPath = "catalog.catalog.csv";
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = k_Stem, CatalogListingId = k_ListingId },
                },
                new List<AssetState>()));
        m_FakeCsvCatalogLoader.WriteException = new IOException("write failed");

        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = k_Stem, CatalogListingId = k_ListingId, ProductType = ProductType.NonConsumable },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(csvPath)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FetchAsync_Reconcile_FilePath_CreatesUcatInParentDir()
    {
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult<(List<CatalogItem>, List<AssetState>)>(
                (new List<CatalogItem>(),
                 new List<AssetState>()));

        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "remote-item", CatalogListingId = CatalogItem.CatalogListingIdPrefix + "remote-item" },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(path: Path.Combine("dir", "catalog.catalog.csv"), reconcile: true),
            [new AuthoringFile(Path.Combine("dir", "catalog.catalog.csv"))],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Created, Has.Count.EqualTo(1));
        var createdPath = m_FakeUcatCatalogLoader.CreateOrUpdateCalls[0].Path;
        Assert.That(createdPath, Is.EqualTo(Path.Combine("dir", "remote-item.ucat")));
    }

    [Test]
    public async Task FetchAsync_CsvWithParseErrors_DoesNotRewriteCsv()
    {
        const string csvPath = "catalog.catalog.csv";
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = k_Stem, CatalogListingId = k_ListingId, ProductType = ProductType.NonConsumable },
                },
                new List<AssetState>
                {
                    new("Row 3 skipped: missing Sku", "missing Sku", SeverityLevel.Error),
                }));

        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = k_Stem, CatalogListingId = k_ListingId, ProductType = ProductType.NonConsumable },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(csvPath)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeCsvCatalogLoader.WriteCatalogPaths, Is.Empty);
        Assert.That(result.Failed, Has.Count.EqualTo(2));
        Assert.That(result.Fetched, Is.Empty);
    }

    [Test]
    public async Task FetchAsync_CsvWithDuplicateId_DoesNotRewriteCsv()
    {
        const string csvPath = "catalog.catalog.csv";
        var ucatEntry = MakeEntry(k_Path);
        ucatEntry.CatalogItem.CatalogListingId = k_ListingId;

        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = "item-a", CatalogListingId = k_ListingId },
                },
                new List<AssetState>()));

        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = k_Stem, CatalogListingId = k_ListingId, ProductType = ProductType.NonConsumable },
        };

        var result = await m_Service!.FetchAsync(
            MakeFetchInput(),
            [new AuthoringFile(csvPath), new AuthoringFile(k_Path)],
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeCsvCatalogLoader.WriteCatalogPaths, Is.Empty);
        Assert.That(result.Failed, Has.Count.EqualTo(2));
        Assert.That(result.Fetched, Is.Empty);
    }
}
