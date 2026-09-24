using NUnit.Framework;
using Unity.Services.Cli.Purchasing.Authoring;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class CatalogCsvConfigFileTests
{
    [Test]
    public void Extension_IsCatalogCsv()
    {
        var template = new CatalogCsvConfigFile();
        Assert.That(template.Extension, Is.EqualTo(".catalog.csv"));
    }

    [Test]
    public void FileBodyText_IsNotEmpty()
    {
        var template = new CatalogCsvConfigFile();
        Assert.That(template.FileBodyText, Is.Not.Empty);
    }

    [Test]
    public void FileBodyText_ContainsExpectedHeaders()
    {
        var template = new CatalogCsvConfigFile();
        Assert.That(template.FileBodyText, Does.Contain("CatalogListingId"));
        Assert.That(template.FileBodyText, Does.Contain("Sku"));
        Assert.That(template.FileBodyText, Does.Contain("ProductType"));
    }

    [Test]
    public void FileBodyText_ContainsExpectedSampleData()
    {
        var template = new CatalogCsvConfigFile();
        Assert.That(template.FileBodyText, Does.Contain("starter_pack"));
        Assert.That(template.FileBodyText, Does.Contain("premium_upgrade"));
        Assert.That(template.FileBodyText, Does.Contain("vip_monthly"));
    }
}
