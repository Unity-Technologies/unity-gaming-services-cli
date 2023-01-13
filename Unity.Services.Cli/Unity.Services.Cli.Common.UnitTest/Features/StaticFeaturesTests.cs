using NUnit.Framework;
using Unity.Services.Cli.Common.Features;

namespace Unity.Services.Cli.Common.UnitTest.Features;

public class StaticFeaturesTests
{
    [Test]
    public void DefaultConstructor_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            var features = new StaticFeatures();
            features.IsEnabled("IDoNotExist");
        });
    }

    [Test]
    public void IsEnabled_WhenFeaturesDoesNotExist_ReturnsFalse()
    {
        var features = new StaticFeatures(new Dictionary<string, bool>());

        Assert.IsFalse(features.IsEnabled("IDoNotExist"));
    }

    [Test]
    public void IsEnabled_WhenFeatureIsDisabled_ReturnsFalse()
    {
        var features = new StaticFeatures(new Dictionary<string, bool>
        {
            { "Feature", false }
        });

        Assert.IsFalse(features.IsEnabled("Feature"));
    }

    [Test]
    public void IsEnabled_WhenFeatureIsEnabled_ReturnsTrue()
    {
        var features = new StaticFeatures(new Dictionary<string, bool>
        {
            { "Feature", true }
        });

        Assert.IsTrue(features.IsEnabled("Feature"));
    }
}
