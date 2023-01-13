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
}
