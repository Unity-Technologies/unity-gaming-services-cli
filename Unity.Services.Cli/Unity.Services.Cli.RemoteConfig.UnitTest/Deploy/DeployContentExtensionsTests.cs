using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;

namespace Unity.Services.Cli.RemoteConfig.UnitTest.Deploy;

public class DeployContentExtensionsTests
{
    [Test]
    public void MergeUniqueDescriptions_IgnoresUniqueDescriptions()
    {
        var content = new List<DeployContent>
        {
            new ("a", "type", "path", detail: "detail A"),
            new ("b", "type", "path", detail: "detail B")
        };

        Assert.That(content.GetUniqueDescriptions(), Has.Count.EqualTo(2));
    }

    [Test]
    public void MergeUniqueDescriptions_MergesDuplicateDescriptions()
    {
        var content = new List<DeployContent>
        {
            new ("a", "type", "path", detail: "detail"),
            new ("b", "type", "path", detail: "detail")
        };

        Assert.That(content.GetUniqueDescriptions(), Has.Count.EqualTo(1));
    }
}
