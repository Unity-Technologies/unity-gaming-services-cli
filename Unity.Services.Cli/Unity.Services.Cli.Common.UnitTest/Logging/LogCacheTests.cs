using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
public class LogCacheTests
{
    LogCache? m_Cache;

    [SetUp]
    public void SetUp()
    {
        m_Cache = new LogCache();
    }

    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void LogCache_HasLoggedMessage(bool addResult, bool addMsg, bool expectedResult)
    {
        if (addResult)
        {
            m_Cache!.AddResult("test string");
        }

        if (addMsg)
        {
            m_Cache!.AddMessage("testMessage1", LogLevel.Information, "testName1");
        }

        Assert.AreEqual(expectedResult, m_Cache!.HasLoggedMessage());
    }

    [TestCase("test string")]
    [TestCase(1)]
    public void LogCache_AddResultCorrectly(object result)
    {
        m_Cache!.AddResult(result);
        Assert.AreEqual(result, m_Cache.Result);
    }

    [TestCase("testMessage1", LogLevel.Information, "testName1")]
    [TestCase("testMessage2", LogLevel.Error, "testName2")]
    public void LogCache_AddMessageCorrectly(string message, LogLevel level, string name)
    {
        var expectedMsg = new LogMessage()
        {
            Message = message,
            Type = level,
            Name = name,
        };
        m_Cache!.AddMessage(message, level, name);
        Assert.IsTrue(m_Cache!.Messages.Exists(
            m =>
                m.Name == expectedMsg.Name &&
                m.Type == expectedMsg.Type &&
                m.Message == expectedMsg.Message
            ));
    }

    [Test]
    public void LogCache_CleanLog()
    {
        const string testResult = "test result";
        m_Cache!.AddResult(testResult);
        Assert.AreEqual(testResult, m_Cache.Result);
        m_Cache!.CleanLog();
        Assert.AreEqual(null, m_Cache.Result);
    }
}
