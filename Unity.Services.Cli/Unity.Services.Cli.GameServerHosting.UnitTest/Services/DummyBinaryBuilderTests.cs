using Unity.Services.Cli.GameServerHosting.Services;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class DummyBinaryBuilderTests
{
    [Test]
    public void BuildLinuxServer_ReturnsPath()
    {
        var build = new DummyBinaryBuilder().BuildLinuxServer("dir", "name");

        Assert.That(build.Path.Contains("dir"));
        Assert.That(build.Path.Contains("name"));
    }

    [Test]
    public void RevertToOriginalBuildTarget_DoesNothing()
    {
        Assert.DoesNotThrow(() =>
        {
            new DummyBinaryBuilder().WarnBuildTargetChanged();
        });
    }
}
