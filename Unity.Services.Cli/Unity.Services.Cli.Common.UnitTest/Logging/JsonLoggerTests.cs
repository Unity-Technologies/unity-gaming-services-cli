using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class JsonLoggerTests
{
    const string k_Message = "Message";

    readonly Logger m_Logger = new();
    LogMessageTestHelper? m_LogMessageTestHelper;

    [SetUp]
    public void SetUp()
    {
        m_Logger.Configuration.IsJson = true;
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
    public void Log_CorrectInformationOutput(LogLevel level)
    {
        m_Logger.Log(level, k_Message);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        var messageList = new List<LogMessage>
        {
            new()
            {
                Name = "",
                Message = k_Message,
                Type = level
            }
        };
        var expectMessage =
            LogMessageTestHelper.GetJsonLogMessage(messageList, null);
        Assert.AreEqual(expectMessage, resultLogMessage);
    }

    [Test]
    public void LogResult_JsonResultOutput()
    {
        m_Logger.LogResultValue(k_Message);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        var messageList = new List<LogMessage>();
        var expectMessage =
            LogMessageTestHelper.GetJsonLogMessage(messageList, k_Message);
        Assert.AreEqual(expectMessage, resultLogMessage);
    }

    [Test]
    public void Log_JsonResultAndInformationOutput()
    {
        var level = LogLevel.Information;
        m_Logger.LogResultValue(k_Message);
        m_Logger.Log(level, k_Message);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        var messageList = new List<LogMessage>
        {
            new()
            {
                Name = "",
                Message = k_Message,
                Type = level
            }
        };
        var expectMessage =
            LogMessageTestHelper.GetJsonLogMessage(messageList, k_Message);
        Assert.AreEqual(expectMessage, resultLogMessage);
    }
}
