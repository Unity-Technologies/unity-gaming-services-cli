using NUnit.Framework;
using Unity.Services.Cli.Purchasing.IO;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Purchasing.UnitTest.IO;

[TestFixture]
class CliCsvCatalogLoaderTests
{
    FakeCatalogCsvParser m_FakeParser = new();
    FakeFileSystem m_FakeFileSystem = new();
    CliCsvCatalogLoader m_Loader = null!;

    [SetUp]
    public void SetUp()
    {
        m_FakeParser = new FakeCatalogCsvParser();
        m_FakeFileSystem = new FakeFileSystem();
        m_Loader = new CliCsvCatalogLoader(m_FakeFileSystem, m_FakeParser);
    }

    [Test]
    public async Task ReadCatalog_ValidCsv_ReturnsEntries()
    {
        m_FakeFileSystem.Files["test.catalog.csv"] = "csv-content";
        m_FakeParser.ParseImpl = _ => (
            new List<CatalogItem>
            {
                new() { uSku = "item-a", CatalogListingId = "catalog/item-a" },
                new() { uSku = "item-b" },
            },
            new List<AssetState>());

        var (entries, failed) = await m_Loader.ReadCatalog("test.catalog.csv", CancellationToken.None);

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(failed, Is.Empty);
        Assert.That(entries[1].CatalogItem!.CatalogListingId, Does.StartWith("catalog/"));
    }

    [Test]
    public async Task ReadCatalog_WithParseIssues_ReturnsFailedItems()
    {
        m_FakeFileSystem.Files["test.catalog.csv"] = "csv-content";
        m_FakeParser.ParseImpl = _ => (
            new List<CatalogItem>
            {
                new() { uSku = "good-item" },
            },
            new List<AssetState>
            {
                new("Row 2 skipped: missing Sku", "Each data row must have a non-empty Sku.", SeverityLevel.Error),
            });

        var (entries, failed) = await m_Loader.ReadCatalog("test.catalog.csv", CancellationToken.None);

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(failed, Has.Count.EqualTo(1));
        Assert.That(failed[0].Path, Is.EqualTo("test.catalog.csv"));
        Assert.That(failed[0].Name, Is.EqualTo("Row 2 skipped: missing Sku"));
    }

    [Test]
    public async Task ReadCatalog_IoException_ReturnsFailedToLoad()
    {
        m_FakeFileSystem.ReadException = new IOException("disk error");

        var (entries, failed) = await m_Loader.ReadCatalog("missing.catalog.csv", CancellationToken.None);

        Assert.That(entries, Is.Empty);
        Assert.That(failed, Has.Count.EqualTo(1));
        Assert.That(failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public async Task ReadCatalog_GeneralException_ReturnsFailedToRead()
    {
        m_FakeFileSystem.Files["bad.catalog.csv"] = "content";
        m_FakeParser.ParseException = new FormatException("bad format");

        var (entries, failed) = await m_Loader.ReadCatalog("bad.catalog.csv", CancellationToken.None);

        Assert.That(entries, Is.Empty);
        Assert.That(failed, Has.Count.EqualTo(1));
        Assert.That(failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public void ReadCatalog_OperationCanceled_Throws()
    {
        m_FakeFileSystem.ReadException = new OperationCanceledException();

        Assert.ThrowsAsync<OperationCanceledException>(
            () => m_Loader.ReadCatalog("test.catalog.csv", CancellationToken.None));
    }

    [Test]
    public async Task ReadCatalog_EntryPath_UsesUcatExtensionInParentDir()
    {
        var csvPath = Path.Combine("subdir", "test.catalog.csv");
        m_FakeFileSystem.Files[csvPath] = "content";
        m_FakeParser.ParseImpl = _ => (
            new List<CatalogItem>
            {
                new() { uSku = "my-product" },
            },
            new List<AssetState>());

        var (entries, _) = await m_Loader.ReadCatalog(csvPath, CancellationToken.None);

        Assert.That(entries[0].Path, Is.EqualTo(Path.Combine("subdir", "test.catalog.csv", "my-product.ucat")));
    }

    [Test]
    public async Task ReadCatalog_AllIssuesNoItems_ReturnsOnlyFailed()
    {
        m_FakeFileSystem.Files["bad.catalog.csv"] = "content";
        m_FakeParser.ParseImpl = _ => (
            new List<CatalogItem>(),
            new List<AssetState>
            {
                new("Row 1 skipped: missing Sku", "Each data row must have a non-empty Sku.", SeverityLevel.Error),
                new("Row 2 skipped: missing Sku", "Each data row must have a non-empty Sku.", SeverityLevel.Error),
            });

        var (entries, failed) = await m_Loader.ReadCatalog("bad.catalog.csv", CancellationToken.None);

        Assert.That(entries, Is.Empty);
        Assert.That(failed, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ReadCatalog_EmptyCsv_ReturnsEmptyLists()
    {
        m_FakeFileSystem.Files["empty.catalog.csv"] = "";
        m_FakeParser.ParseImpl = _ => (new List<CatalogItem>(), new List<AssetState>());

        var (entries, failed) = await m_Loader.ReadCatalog("empty.catalog.csv", CancellationToken.None);

        Assert.That(entries, Is.Empty);
        Assert.That(failed, Is.Empty);
    }
}
