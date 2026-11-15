using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
public class PreviewFeaturesTests
{
    static ISystemEnvironmentProvider Environment(string? value)
    {
        var environment = new Mock<ISystemEnvironmentProvider>();
        var error = "";
        environment.Setup(e => e.GetSystemEnvironmentVariable(Keys.EnvironmentKeys.Preview, out error)).Returns(value);
        return environment.Object;
    }

    [TestCase("player-id-providers")]
    [TestCase("PLAYER-ID-PROVIDERS")]
    [TestCase("other, player-id-providers")]
    [TestCase("other;player-id-providers unknown")]
    [TestCase(" player-id-providers ")]
    public void IsEnabled_FindsTheFeatureInTheList(string value)
    {
        Assert.That(PreviewFeatures.IsEnabled(PreviewFeatures.PlayerIdProviders, Environment(value)), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("player-id-provider")]
    [TestCase("player-id-providers-extra")]
    [TestCase("unknown")]
    public void IsEnabled_IsFalseUnlessTheExactNameIsListed(string? value)
    {
        Assert.That(PreviewFeatures.IsEnabled(PreviewFeatures.PlayerIdProviders, Environment(value)), Is.False);
    }
}
