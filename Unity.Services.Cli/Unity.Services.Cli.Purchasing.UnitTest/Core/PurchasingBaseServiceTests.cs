using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Purchasing.Authoring;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.UnitTest.Core;

[TestFixture]
class PurchasingBaseServiceTests
{
    [Test]
    public void FilterByExtension_ReturnsOnlyMatchingFiles()
    {
        var files = new[] { "item.ucat", "catalog.catalog.csv", "readme.txt", "data.json" };

        var result = TestableBaseService.CallFilterByExtension(files, Constants.FileExtension);

        Assert.That(result, Is.EqualTo(new[] { "item.ucat" }));
    }

    [Test]
    public void FilterByExtension_CaseInsensitive()
    {
        var files = new[] { "ITEM.UCAT", "item.Ucat", "item.txt" };

        var result = TestableBaseService.CallFilterByExtension(files, Constants.FileExtension);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void FilterByExtension_NoMatches_ReturnsEmpty()
    {
        var files = new[] { "readme.txt", "data.json" };

        var result = TestableBaseService.CallFilterByExtension(files, Constants.FileExtension);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FilterByExtension_CsvExtension()
    {
        var files = new[] { "item.ucat", "catalog.catalog.csv", "other.catalog.csv" };

        var result = TestableBaseService.CallFilterByExtension(files, Constants.CsvFileExtension);

        Assert.That(result, Is.EqualTo(new[] { "catalog.catalog.csv", "other.catalog.csv" }));
    }

    class TestableBaseService : PurchasingBaseService
    {
        public TestableBaseService()
            : base(
                new Mock<ILiveContentConfigClient>().Object,
                new Mock<ICatalogUcatLoader>().Object,
                new Mock<ICatalogCsvLoader>().Object)
        {
        }

        public static IReadOnlyList<string> CallFilterByExtension(
            IReadOnlyList<string> filePaths, string extension)
            => FilterByExtension(filePaths, extension);
    }
}
