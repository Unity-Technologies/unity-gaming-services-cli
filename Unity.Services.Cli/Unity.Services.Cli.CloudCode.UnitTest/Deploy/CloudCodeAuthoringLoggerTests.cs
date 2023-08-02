using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

[TestFixture]
class CloudCodeAuthoringLoggerTests
{
    readonly Mock<ILogger> m_MockLogger = new();
    readonly CloudCodeAuthoringLogger m_CloudCodeAuthoringLogger;

    public CloudCodeAuthoringLoggerTests()
    {
        m_CloudCodeAuthoringLogger = new(m_MockLogger.Object);
    }

    [SetUp]
    public void SetUp()
    {
        m_MockLogger.Reset();
    }

    [Test]
    public void LogErrorLoggerLogWithErrorLevel_NoOpLogger()
    {
        m_CloudCodeAuthoringLogger.LogError("error message");
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Error, null, Times.Never);
    }

    [Test]
    public void LogLogInfoLoggerLogWithInformationLevel_NoOpLogger()
    {
        m_CloudCodeAuthoringLogger.LogInfo("information message");
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, null, Times.Never);
    }

    [Test]
    public void LogLogWarningLoggerLogWithWarningLevel_NoOpLogger()
    {
        m_CloudCodeAuthoringLogger.LogWarning("warning message");
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Warning, null, Times.Never);
    }
}
