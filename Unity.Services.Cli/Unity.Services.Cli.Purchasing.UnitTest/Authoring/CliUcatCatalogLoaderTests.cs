using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using Unity.Services.Cli.Purchasing.IO;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class CliUcatCatalogLoaderTests
{
    FakeFileSystem m_FakeFileSystem = new();
    CliUcatCatalogLoader? m_Loader;

    [SetUp]
    public void SetUp()
    {
        m_FakeFileSystem = new FakeFileSystem();
        m_Loader = new CliUcatCatalogLoader(m_FakeFileSystem);
    }

    [Test]
    public async Task ReadCatalog_ReturnsItemWithCorrectSku()
    {
        const string path = "/data/my-item.ucat";
        m_FakeFileSystem.Files[path] =
            JsonConvert.SerializeObject(new { uSku = "my-item" });

        var result = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(result.CatalogItem?.uSku, Is.EqualTo("my-item"));
        Assert.That(
            result.CatalogItem?.CatalogListingId,
            Is.EqualTo(CatalogItem.CatalogListingIdPrefix + "my-item"));
    }

    [Test]
    public async Task ReadCatalog_WhenSkuNullInFile_UsesFileStem()
    {
        const string path = "/data/stem-name.ucat";
        m_FakeFileSystem.Files[path] =
            JsonConvert.SerializeObject(new { ProductType = "Consumable" });

        var result = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(result.CatalogItem?.uSku, Is.EqualTo("stem-name"));
    }

    [Test]
    public async Task ReadCatalog_OnIoException_SetsFailedToLoadStatus()
    {
        const string path = "/data/missing.ucat";
        m_FakeFileSystem.ReadException = new IOException("file not found");

        var result = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(result.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public async Task ReadCatalog_OnJsonException_SetsFailedToReadStatus()
    {
        const string path = "/data/corrupt.ucat";
        m_FakeFileSystem.Files[path] = "{ not valid json {{";

        var result = await m_Loader!.ReadCatalog(path, CancellationToken.None);

        Assert.That(result.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    [Test]
    public async Task CreateOrUpdateCatalog_WritesSerializedJson()
    {
        const string path = "/data/item.ucat";
        var entry = new CatalogEntryDeploymentItem(path)
        {
            CatalogItem = new CatalogItem
            {
                uSku = "item",
                CatalogListingId = "catalog/item",
            },
        };

        await m_Loader!.CreateOrUpdateCatalog(entry, CancellationToken.None);

        Assert.That(m_FakeFileSystem.WrittenPaths, Contains.Item(path));
        Assert.That(m_FakeFileSystem.WrittenContents[path], Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task CreateOrUpdateCatalog_StripsSkuFromJson_WhenSkuMatchesStem()
    {
        const string path = "/data/my-sku.ucat";
        var entry = new CatalogEntryDeploymentItem(path)
        {
            CatalogItem = new CatalogItem
            {
                uSku = "my-sku",
                CatalogListingId = "catalog/my-sku",
            },
        };

        await m_Loader!.CreateOrUpdateCatalog(entry, CancellationToken.None);

        var written = m_FakeFileSystem.WrittenContents[path];
        Assert.That(written, Does.Not.Contain("\"uSku\""));
    }

    [Test]
    public async Task DeleteCatalog_CallsFileSystemDelete()
    {
        const string path = "/data/item.ucat";
        var entry = new CatalogEntryDeploymentItem(path);

        await m_Loader!.DeleteCatalog(entry, CancellationToken.None);

        Assert.That(m_FakeFileSystem.DeletedPaths, Contains.Item(path));
    }

    [Test]
    public async Task DeleteCatalog_OnIoException_SetsErrorStatus()
    {
        const string path = "/data/item.ucat";
        m_FakeFileSystem.DeleteException = new IOException("cannot delete");

        var entry = new CatalogEntryDeploymentItem(path);
        await m_Loader!.DeleteCatalog(entry, CancellationToken.None);

        Assert.That(entry.Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }
}
