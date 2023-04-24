using System.IO;
using NUnit.Framework;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class IoUtilsTests
{
    [Test]
    public void NormalizePath()
    {
        var rawPath = $"foo{Path.AltDirectorySeparatorChar}bar{Path.DirectorySeparatorChar}file.extension";
        var expectedNormalizedPath = $"foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}file.extension";

        var normalizedPath = IoUtils.NormalizePath(rawPath);

        Assert.That(normalizedPath, Is.EqualTo(expectedNormalizedPath));
    }
}
