using Moq;
using NUnit.Framework;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Deploy;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using CoreLogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

[TestFixture]
class PurchasingDeploymentHandlerTests
{
    FakeClient m_FakeClient = new();
    CatalogDeploymentHandler? m_Handler;

    [SetUp]
    public void SetUp()
    {
        m_FakeClient = new FakeClient();
        m_Handler = new CatalogDeploymentHandler(m_FakeClient, new Mock<IWebshopCategoriesClient>().Object, new Mock<CoreLogger.ILogger>().Object);
    }

    [Test]
    public async Task DeployAsync_UpsertsEachEntry()
    {
        var entries = new List<CatalogEntryDeploymentItem>
        {
            MakeValidEntry("item1"),
            MakeValidEntry("item2"),
        };

        await m_Handler!.DeployAsync(
            entries, dryRun: false, reconcile: false, CancellationToken.None);

        Assert.That(m_FakeClient.UpsertedItems.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task DeployAsync_DryRun_DoesNotCallUpsert()
    {
        var entries = new List<CatalogEntryDeploymentItem>
        {
            MakeValidEntry("item1"),
        };

        await m_Handler!.DeployAsync(
            entries, dryRun: true, reconcile: false, CancellationToken.None);

        Assert.That(m_FakeClient.UpsertedItems, Is.Empty);
    }

    [Test]
    public async Task DeployAsync_Reconcile_DeletesRemoteItemsNotInLocal()
    {
        m_FakeClient.RemoteItems.Add(new CatalogItem
        {
            uSku = "remote-only",
            CatalogListingId = "catalog/remote-only",
        });

        // Provide a local entry so the handler can derive a rootDirectory; it has a
        // different SKU than the remote item so "remote-only" is still orphaned.
        var localEntries = new List<CatalogEntryDeploymentItem>
        {
            MakeValidEntry("local-only"),
        };

        await m_Handler!.DeployAsync(
            localEntries,
            dryRun: false,
            reconcile: true,
            CancellationToken.None);

        Assert.That(m_FakeClient.DeletedItems.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Creates a <see cref="CatalogEntryDeploymentItem"/> that passes handler validation
    /// (valid uSku, at least one USD pricing entry, and at least one product detail with title).
    /// </summary>
    static CatalogEntryDeploymentItem MakeValidEntry(string stem)
    {
        return new CatalogEntryDeploymentItem(stem + Constants.FileExtension)
        {
            CatalogItem = new CatalogItem
            {
                uSku = stem,
                CatalogListingId = CatalogItem.CatalogListingIdPrefix + stem,
                ProductType = ProductType.Consumable,
                PricingDetails = new List<PricingDetails>
                {
                    new PricingDetails { CurrencyCode = "USD", Amount = 0.99 },
                },
                ProductDetails = new List<ProductDetails>
                {
                    new ProductDetails { Title = "Test Item", Language = TranslationLocale.en_US },
                },
            },
        };
    }
}
