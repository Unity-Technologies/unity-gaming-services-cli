using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Common.Telemetry;

namespace Unity.Services.Cli.Common.UnitTest.Telemetry;

[TestFixture]
public class TelemetryTests
{
    readonly Mock<ISystemEnvironmentProvider> m_MockSystemEnvironmentProvider;

    public TelemetryTests()
    {
        m_MockSystemEnvironmentProvider = new Mock<ISystemEnvironmentProvider>();
    }

    [SetUp]
    public void SetUp()
    {
        m_MockSystemEnvironmentProvider.Reset();
    }

    [Test]
    public void GetCicdPlatform_NoPlatformReturnsEmptyString()
    {
        var platform = TelemetryConfigurationProvider.GetCicdPlatform(m_MockSystemEnvironmentProvider.Object);
        StringAssert.AreEqualIgnoringCase(platform, "");
    }

    [Test]
    public void GetCicdPlatform_ValidPlatformReturnsValue()
    {
        string? errorMsg = null;
        string expectedPlatform = Keys.CicdEnvVarToDisplayNamePair[Keys.EnvironmentKeys.RunningOnDocker];
        m_MockSystemEnvironmentProvider.Setup(ex => ex
                .GetSystemEnvironmentVariable(Keys.EnvironmentKeys.RunningOnDocker, out errorMsg))
            .Returns(expectedPlatform);
        var platform = TelemetryConfigurationProvider.GetCicdPlatform(m_MockSystemEnvironmentProvider.Object);
        StringAssert.AreEqualIgnoringCase(expectedPlatform, platform);
    }

    void SetEnvironmentVariable(string key, string value)
    {
        string? errorMsg = null;
        m_MockSystemEnvironmentProvider.Setup(ex => ex
                .GetSystemEnvironmentVariable(key, out errorMsg))
            .Returns(value);
    }

    [Test]
    public void IsRunByUnityCli_FalseWithoutContext()
    {
        Assert.IsFalse(TelemetryConfigurationProvider.IsRunByUnityCli(m_MockSystemEnvironmentProvider.Object));
    }

    [Test]
    public void IsRunByUnityCli_TrueWithContextVersion()
    {
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliContextVersion, "1");
        Assert.IsTrue(TelemetryConfigurationProvider.IsRunByUnityCli(m_MockSystemEnvironmentProvider.Object));
    }

    [Test]
    public void IsAnalyticsAllowed_WithoutContext_DefaultsToAllowed()
    {
        Assert.IsTrue(TelemetryConfigurationProvider.IsAnalyticsAllowed(m_MockSystemEnvironmentProvider.Object));
    }

    [Test]
    public void IsAnalyticsAllowed_WithoutContext_RespectsTelemetryDisabled()
    {
        SetEnvironmentVariable(Keys.EnvironmentKeys.TelemetryDisabled, "1");
        Assert.IsFalse(TelemetryConfigurationProvider.IsAnalyticsAllowed(m_MockSystemEnvironmentProvider.Object));
    }

    [TestCase("1", true)]
    [TestCase(" 1 ", true)]
    [TestCase("0", false)]
    [TestCase("", false)]
    [TestCase("true", false)]
    public void IsAnalyticsAllowed_UnderUnityCli_FollowsAnalyticsEnabled(string value, bool expected)
    {
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliContextVersion, "1");
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliAnalyticsEnabled, value);
        Assert.AreEqual(expected, TelemetryConfigurationProvider.IsAnalyticsAllowed(m_MockSystemEnvironmentProvider.Object));
    }

    [Test]
    public void IsAnalyticsAllowed_UnderUnityCli_MissingValueIsNotAllowed()
    {
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliContextVersion, "1");
        Assert.IsFalse(TelemetryConfigurationProvider.IsAnalyticsAllowed(m_MockSystemEnvironmentProvider.Object));
    }

    [Test]
    public void IsAnalyticsAllowed_UnderUnityCli_TelemetryDisabledStillWins()
    {
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliContextVersion, "1");
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliAnalyticsEnabled, "1");
        SetEnvironmentVariable(Keys.EnvironmentKeys.TelemetryDisabled, "1");
        Assert.IsFalse(TelemetryConfigurationProvider.IsAnalyticsAllowed(m_MockSystemEnvironmentProvider.Object));
    }

    [TestCase("1", false)]
    [TestCase("0", true)]
    public void AnalyticEvent_UnderUnityCli_FollowsAnalyticsEnabled(string value, bool expectedDisabled)
    {
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliContextVersion, "1");
        SetEnvironmentVariable(Keys.EnvironmentKeys.UnityCliAnalyticsEnabled, value);

        var analyticEvent = new Common.Telemetry.AnalyticEvent.AnalyticEvent(
            m_MockSystemEnvironmentProvider.Object, "test.event");

        Assert.AreEqual(expectedDisabled, analyticEvent.IsDisabled);
    }
}
