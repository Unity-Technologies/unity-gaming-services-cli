using NUnit.Framework;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using Unity.Services.Cli.Purchasing.IO;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class CliCsvCatalogLoaderTests
{
    FakeFileSystem m_FakeFileSystem = new();
    FakeCatalogCsvParser m_FakeCsvParser = new();
    CliCsvCatalogLoader? m_Loader;

    [SetUp]
    public void SetUp()
    {
        m_FakeFileSystem = new FakeFileSystem();
        m_FakeCsvParser = new FakeCatalogCsvParser();
        m_Loader = new CliCsvCatalogLoader(m_FakeFileSystem, m_FakeCsvParser);
    }

    [Test]
    public async Task ReadCatalog_ReturnsParsedEntries()
    {
        const string path = "/data/catalog.catalog.csv";
        m_FakeFileSystem.Files[path] = "csv-content";
        m_FakeCsvParser.ParseImpl = _ =>
            (new List<CatalogItem>
            {
                new() { uSku = "item-a" },
                new() { uSku = "item-b" },
            }, new List<AssetState>());

        var (entries, failed) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(failed, Is.Empty);
    }

    [Test]
    public async Task ReadCatalog_SetsCatalogListingId_WhenMissing()
    {
        const string path = "/data/catalog.catalog.csv";
        m_FakeFileSystem.Files[path] = "csv-content";
        m_FakeCsvParser.ParseImpl = _ =>
            (new List<CatalogItem>
            {
                new() { uSku = "sku-1" },
            }, new List<AssetState>());

        var (entries, _) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(
            entries[0].CatalogItem.CatalogListingId,
            Is.EqualTo(CatalogItem.CatalogListingIdPrefix + "sku-1"));
    }

    [Test]
    public async Task ReadCatalog_PreservesCatalogListingId_WhenAlreadySet()
    {
        const string path = "/data/catalog.catalog.csv";
        m_FakeFileSystem.Files[path] = "csv-content";
        m_FakeCsvParser.ParseImpl = _ =>
            (new List<CatalogItem>
            {
                new() { uSku = "sku-1", CatalogListingId = "custom/id" },
            }, new List<AssetState>());

        var (entries, _) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(entries[0].CatalogItem.CatalogListingId, Is.EqualTo("custom/id"));
    }

    [Test]
    public async Task ReadCatalog_SetsPathWithCsvAsParentDir()
    {
        const string path = "/data/catalog.catalog.csv";
        m_FakeFileSystem.Files[path] = "csv-content";
        m_FakeCsvParser.ParseImpl = _ =>
            (new List<CatalogItem>
            {
                new() { uSku = "item" },
            }, new List<AssetState>());

        var (entries, _) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(entries[0].Path, Is.EqualTo(Path.Combine(path, "item.ucat")));
    }

    [Test]
    public async Task ReadCatalog_WithParseIssues_ReportsFailedItems()
    {
        const string path = "/data/catalog.catalog.csv";
        m_FakeFileSystem.Files[path] = "csv-content";
        m_FakeCsvParser.ParseImpl = _ =>
            (new List<CatalogItem>
            {
                new() { uSku = "good-item" },
            }, new List<AssetState>
            {
                new("bad-row", "Invalid row format", SeverityLevel.Error),
            });

        var (entries, failed) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(failed, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ReadCatalog_AllItemsFailed_ReturnsOnlyFailures()
    {
        const string path = "/data/catalog.catalog.csv";
        m_FakeFileSystem.Files[path] = "csv-content";
        m_FakeCsvParser.ParseImpl = _ =>
            (new List<CatalogItem>(),
             new List<AssetState>
             {
                 new("row-1", "parse error", SeverityLevel.Error),
             });

        var (entries, failed) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(entries, Is.Empty);
        Assert.That(failed, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ReadCatalog_OnIoException_SetsFailedToLoadStatus()
    {
        const string path = "/data/missing.catalog.csv";
        m_FakeFileSystem.ReadException = new IOException("file not found");

        var (entries, failed) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(entries, Is.Empty);
        Assert.That(failed, Has.Count.EqualTo(1));
        Assert.That(failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public async Task ReadCatalog_OnGenericException_SetsFailedToReadStatus()
    {
        const string path = "/data/bad.catalog.csv";
        m_FakeCsvParser.ParseException = new FormatException("bad format");
        m_FakeFileSystem.Files[path] = "csv-content";

        var (entries, failed) = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(entries, Is.Empty);
        Assert.That(failed, Has.Count.EqualTo(1));
        Assert.That(failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public async Task WriteCatalog_SerializesAndWritesToFile()
    {
        const string path = "/data/catalog.catalog.csv";
        var items = new List<CatalogItem>
        {
            new() { uSku = "item-a" },
        };

        await m_Loader!.WriteCatalog(path, items, CancellationToken.None);

        Assert.That(m_FakeFileSystem.WrittenPaths, Contains.Item(path));
    }

    [Test]
    public async Task DeleteCatalog_CallsFileSystemDelete()
    {
        const string path = "/data/catalog.catalog.csv";

        await m_Loader!.DeleteCatalog(path, CancellationToken.None);

        Assert.That(m_FakeFileSystem.DeletedPaths, Contains.Item(path));
    }

    [Test]
    public void ReadCatalog_OnCancellation_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        m_FakeFileSystem.ReadException = new OperationCanceledException(cts.Token);

        Assert.ThrowsAsync<OperationCanceledException>(
            () => m_Loader!.ReadCatalog("/data/any.catalog.csv", cts.Token));
    }
}
