using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Purchasing.Authoring;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class PurchasingAuthoringLoggerTests
{
    Mock<ILogger> m_Logger = null!;
    PurchasingAuthoringLogger m_AuthoringLogger = null!;

    [SetUp]
    public void SetUp()
    {
        m_Logger = new Mock<ILogger>();
        m_AuthoringLogger = new PurchasingAuthoringLogger(m_Logger.Object);
    }

    [Test]
    public void LogError_WithException_LogsOnlyExceptionMessage()
    {
        m_AuthoringLogger.LogError(new InvalidOperationException("server error"));

        VerifyError("server error");
    }

    [Test]
    public void LogError_WithNonException_LogsOriginalMessage()
    {
        m_AuthoringLogger.LogError("server error");

        VerifyError("server error");
    }

    void VerifyError(string expectedMessage)
    {
        m_Logger.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString() == expectedMessage),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
