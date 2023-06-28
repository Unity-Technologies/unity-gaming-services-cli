using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class JsonLoggerTests
{
    const string k_Result = "Message";

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
        m_Logger.Log(level, k_Result);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        var messageList = new List<LogMessage>
        {
            new()
            {
                Name = "",
                Message = k_Result,
                Type = level
            }
        };
        var expectMessage = LogMessageTestHelper.GetJsonLogFormatted(null, messageList);
        Assert.AreEqual(expectMessage, resultLogMessage);
    }

    [Test]
    public void LogResult_JsonResultOutput()
    {
        m_Logger.LogResultValue(k_Result);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        var messageList = new List<LogMessage>();
        var expectMessage = LogMessageTestHelper.GetJsonLogFormatted(k_Result, messageList);
        Assert.AreEqual(expectMessage, resultLogMessage);
    }

    [Test]
    public void Log_JsonResultAndInformationOutput()
    {
        var level = LogLevel.Information;
        m_Logger.LogResultValue(k_Result);
        m_Logger.Log(level, k_Result);
        m_Logger.Write();
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        var messageList = new List<LogMessage>
        {
            new()
            {
                Name = "",
                Message = k_Result,
                Type = level
            }
        };
        var expectMessage = LogMessageTestHelper.GetJsonLogFormatted(k_Result, messageList);
        Assert.AreEqual(expectMessage, resultLogMessage);
    }
}
