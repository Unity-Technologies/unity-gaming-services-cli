using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Purchasing.UnitTest;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

abstract class PurchasingDeployFetchTestBase
{
    protected FakeDeploymentHandler m_FakeDeploymentHandler = new();
    protected FakeClient m_FakeClient = new();
    protected FakeUcatCatalogLoader m_FakeUcatCatalogLoader = new();
    protected FakeCsvCatalogLoader m_FakeCsvCatalogLoader = new();

    [SetUp]
    public virtual void SetUp()
    {
        m_FakeDeploymentHandler = new FakeDeploymentHandler();
        m_FakeClient = new FakeClient();
        m_FakeUcatCatalogLoader = new FakeUcatCatalogLoader();
        m_FakeCsvCatalogLoader = new FakeCsvCatalogLoader();
    }

    protected static CatalogEntryDeploymentItem MakeEntry(string path, bool asError = false)
    {
        var stem = Path.GetFileNameWithoutExtension(path);
        var item = new CatalogEntryDeploymentItem(path)
        {
            CatalogItem = new CatalogItem
            {
                uSku = stem,
                CatalogListingId = CatalogItem.CatalogListingIdPrefix + stem,
            },
        };
        if (asError)
            item.Status = Statuses.GetFailedToLoad(new IOException("test error"), path);
        return item;
    }

    protected static DeployInput MakeDeployInput(bool dryRun = false, bool reconcile = false)
    {
        return new DeployInput
        {
            Paths = Array.Empty<string>(),
            CloudProjectId = "test-project",
            DryRun = dryRun,
            Reconcile = reconcile,
        };
    }

    protected static FetchInput MakeFetchInput(
        string path = "dir",
        bool dryRun = false,
        bool reconcile = false)
    {
        return new FetchInput
        {
            Path = path,
            CloudProjectId = "test-project",
            DryRun = dryRun,
            Reconcile = reconcile,
        };
    }
}
