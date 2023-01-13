using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest.LogFormatter;

[TestFixture]
public class LogFormatterTests
{
    LogMessageTestHelper? m_LogMessageTestHelper;
    LogCache? m_Cache;
    Logging.LogFormatter? m_Formatter;

    [SetUp]
    public void SetUp()
    {
        m_LogMessageTestHelper = new LogMessageTestHelper();
        m_Cache = new LogCache();
        m_Formatter = new Logging.LogFormatter();
    }

    [TearDown]
    public void TearDown()
    {
        m_LogMessageTestHelper!.Dispose();
    }

    [Test]
    public void WriteResult_OutputsString()
    {
        string result = "test output";
        m_Cache!.AddResult(result);
        m_Formatter!.WriteLog(m_Cache);
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        Assert.AreEqual($"{result}{System.Environment.NewLine}", resultLogMessage);
    }

    [Test]
    public void WriteResult_OutputsStringList()
    {
        List<string> result = new List<string>
        {
            "result1",
            "result2"
        };
        m_Cache!.AddResult(result);
        m_Formatter!.WriteLog(m_Cache);
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        Assert.AreEqual($"{result[0]}{System.Environment.NewLine}{result[1]}{System.Environment.NewLine}",
            resultLogMessage);
    }

    [Test]
    public void WriteMessage_OutputsCorrectFormat()
    {
        var msgInfo = new LogMessage()
        {
            Name = "TestName",
            Message = "TestMessage",
            Type = LogLevel.Information
        };
        m_Cache!.AddMessage("TestMessage", LogLevel.Information, "TestName");
        m_Formatter!.WriteLog(m_Cache);
        var resultLogMessage = m_LogMessageTestHelper!.LogMessage;
        Assert.AreEqual($"[{msgInfo.Type.ToString()}]: {msgInfo.Name}{System.Environment.NewLine}    {msgInfo.Message}{System.Environment.NewLine}",
            resultLogMessage);
    }
}
