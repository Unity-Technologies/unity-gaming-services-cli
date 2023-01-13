using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class LoggerTests
{
    const string k_LoggerName = "TestLogger";
    const string k_Message = "Result Message";

    readonly Logger m_Logger = new(k_LoggerName, new LogConfiguration());
    LogMessageTestHelper? m_LogMessageTestHelper;

    [SetUp]
    public void SetUp()
    {
        m_LogMessageTestHelper = new LogMessageTestHelper();
    }

    [TearDown]
    public void TearDown()
    {
        m_LogMessageTestHelper!.Dispose();
    }

    [TestCase(LogLevel.Information)]
    [TestCase(LogLevel.Warning)]
    [TestCase(LogLevel.Error)]
    [TestCase(LogLevel.Critical)]
    public void Log_CorrectLevelOutput(LogLevel level)
    {
        m_Logger.Log(level, k_Message);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        Assert.AreEqual($"[{level}]: {k_LoggerName}{System.Environment.NewLine}    {k_Message}{System.Environment.NewLine}",
            resultLogMessage);
    }

    [Test]
    public void LogResult_ResultOutput()
    {
        m_Logger.LogResultValue(k_Message);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        Assert.AreEqual($"{k_Message}{System.Environment.NewLine}", resultLogMessage);
    }

    [TestCase(1000, null)]
    [TestCase(1000, "FakeResult")]
    [TestCase(1001, LoggerExtension.ResultEventName)]
    public void Log_EventIdSameAsResult_LogAsMessage(int id, string name)
    {
        m_Logger.Log(LogLevel.Critical, new EventId(id, name), k_Message);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        Assert.AreEqual(
            $"[{LogLevel.Critical}]: {k_LoggerName}{System.Environment.NewLine}    {k_Message}{System.Environment.NewLine}",
            resultLogMessage);
    }

    [Test]
    public void Log_ResultId_LogAsResult()
    {
        m_Logger.Log(LogLevel.Critical, LoggerExtension.ResultEventId, k_Message);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        Assert.AreEqual($"{k_Message}{System.Environment.NewLine}", resultLogMessage);
    }

    [TestCase(LogLevel.Error)]
    [TestCase(LogLevel.Critical)]
    public void Log_QuietModeFilterCorrectLevels(LogLevel level)
    {
        m_Logger.Configuration.IsQuiet = true;
        m_Logger.LogWarning("Warning Message");
        m_Logger.Write();
        Assert.AreEqual(string.Empty, m_LogMessageTestHelper!.LogMessage);

        m_Logger.Log(level, k_Message);
        m_Logger.Write();
        Assert.IsTrue(m_LogMessageTestHelper!.LogMessage.Contains(k_Message));
    }
}
