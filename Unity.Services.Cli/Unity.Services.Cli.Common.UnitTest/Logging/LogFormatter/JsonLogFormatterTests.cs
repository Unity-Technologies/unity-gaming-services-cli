using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest.LogFormatter;

[TestFixture]
public class JsonLogFormatterTests
{
    LogMessageTestHelper? m_LogMessageTestHelper;
    LogCache? m_Cache;
    JsonLogFormatter? m_Formatter;

    static readonly List<object> k_WriteResultTestCases = new()
    {
        new List<string>
        {
            "result1",
            "result2"
        },
        "test output"
    };

    [SetUp]
    public void SetUp()
    {
        m_LogMessageTestHelper = new LogMessageTestHelper();

        m_Cache = new LogCache();
        m_Formatter = new JsonLogFormatter();
    }

    [TearDown]
    public void TearDown()
    {
        m_LogMessageTestHelper!.Dispose();
    }

    [TestCaseSource(nameof(k_WriteResultTestCases))]
    [TestCase(null)]
    public void WriteResult_OutputsStringOrStringListCorrectly(object result)
    {
        m_Cache!.AddResult(result);
        m_Formatter!.WriteLog(m_Cache);
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        var expectedLog = LogMessageTestHelper.GetJsonLogMessage(null, result);
        Assert.AreEqual(expectedLog, resultLogMessage);
    }

    [TestCase(LogLevel.Information)]
    [TestCase(LogLevel.Critical)]
    [TestCase(LogLevel.Error)]
    [TestCase(LogLevel.Warning)]
    public void WriteMessage_OutputsCorrectFormat(LogLevel level)
    {
        var msgInfo = new LogMessage()
        {
            Name = "TestName",
            Message = "TestMessage",
            Type = level
        };
        m_Cache!.AddMessage("TestMessage", level, "TestName");
        m_Formatter!.WriteLog(m_Cache);
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        List<LogMessage> messages = new List<LogMessage>();
        messages.Add(msgInfo);
        var expectedLog = LogMessageTestHelper.GetJsonLogMessage(messages, null);
        Assert.AreEqual(expectedLog, resultLogMessage);
    }
}
