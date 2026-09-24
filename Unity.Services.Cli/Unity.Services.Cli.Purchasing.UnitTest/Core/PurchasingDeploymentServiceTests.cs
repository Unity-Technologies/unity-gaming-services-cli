using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Purchasing.Authoring;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

[TestFixture]
class PurchasingDeploymentServiceTests : PurchasingDeployFetchTestBase
{
    PurchasingDeploymentService? m_Service;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        m_FakeUcatCatalogLoader.ReadCatalogImpl =
            (path, _) => Task.FromResult(MakeEntry(path));
        m_Service = new PurchasingDeploymentService(
            m_FakeDeploymentHandler,
            m_FakeClient,
            m_FakeUcatCatalogLoader,
            m_FakeCsvCatalogLoader);
    }

    [Test]
    public async Task Deploy_CallsClientInitialize()
    {
        const string envId = "env-123";
        const string projId = "proj-456";

        await m_Service!.Deploy(
            MakeDeployInput(),
            Array.Empty<AuthoringFile>(),
            projId,
            envId,
            null,
            CancellationToken.None);

        Assert.That(m_FakeClient.LastEnvironmentId, Is.EqualTo(envId));
        Assert.That(m_FakeClient.LastProjectId, Is.EqualTo(projId));
        Assert.That(m_FakeClient.InitializeCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Deploy_CallsReadCatalogForUcatFiles()
    {
        var files = new[]
        {
            new AuthoringFile("item1.ucat"),
            new AuthoringFile("item2.ucat"),
        };

        await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeUcatCatalogLoader.ReadCatalogCallCount, Is.EqualTo(2));
    }

    [Test]
    public async Task Deploy_CallsDeployHandler()
    {
        var files = new[] { new AuthoringFile("item.ucat") };

        await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeDeploymentHandler.DeployCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Deploy_WithFailedToLoad_IncludesFailedInResult()
    {
        m_FakeUcatCatalogLoader.ReadCatalogImpl = (path, _) =>
            Task.FromResult(MakeEntry(path, asError: true));

        var files = new[] { new AuthoringFile("bad.ucat") };

        var result = await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Deploy_OnClientException_StillReturnsResult()
    {
        m_FakeDeploymentHandler.DeployException =
            new ClientException("server error", null);

        var files = new[] { new AuthoringFile("item.ucat") };

        var result = await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task Deploy_CsvFile_ParsesAndDeploysEntries()
    {
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = "item-a", CatalogListingId = CatalogItem.CatalogListingIdPrefix + "item-a" },
                    new() { uSku = "item-b", CatalogListingId = CatalogItem.CatalogListingIdPrefix + "item-b" },
                },
                new List<AssetState>()));

        var files = new[] { new AuthoringFile("catalog.catalog.csv") };

        await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeDeploymentHandler.DeployCallCount, Is.EqualTo(1));
        Assert.That(m_FakeDeploymentHandler.LastEntries?.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Deploy_InvalidCsvFile_IncludesFailedInResult()
    {
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromException<(List<CatalogItem>, List<AssetState>)>(
                new IOException("file not found"));

        var files = new[] { new AuthoringFile("missing.catalog.csv") };

        var result = await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Deploy_CsvParseException_IncludesFailedInResult()
    {
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>(),
                new List<AssetState>
                {
                    new("Invalid CSV", "bad format", SeverityLevel.Error),
                }));

        var files = new[] { new AuthoringFile("bad.catalog.csv") };

        var result = await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Deploy_CsvWithNoValidItems_IncludesFailedInResult()
    {
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>(),
                new List<AssetState>
                {
                    new("missing Sku", "Row 2 skipped: missing Sku", SeverityLevel.Error),
                }));

        var files = new[] { new AuthoringFile("malformed.catalog.csv") };

        var result = await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(result.Failed.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Deploy_MixedUcatAndCsv_AllDeployed()
    {
        m_FakeCsvCatalogLoader.ReadCatalogImpl = (_, _) =>
            Task.FromResult((
                new List<CatalogItem>
                {
                    new() { uSku = "csv-item", CatalogListingId = CatalogItem.CatalogListingIdPrefix + "csv-item" },
                },
                new List<AssetState>()));

        var files = new[]
        {
            new AuthoringFile("item.ucat"),
            new AuthoringFile("catalog.catalog.csv"),
        };

        await m_Service!.Deploy(
            MakeDeployInput(),
            files,
            "proj",
            "env",
            null,
            CancellationToken.None);

        Assert.That(m_FakeDeploymentHandler.LastEntries?.Count, Is.EqualTo(2));
    }
}
