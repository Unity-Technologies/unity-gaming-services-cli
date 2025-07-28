using NUnit.Framework;
using Unity.Services.GameServerHosting.LocalProxy.Model;

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.Model
{
    public class TestLogger : ILogger
    {
        public void Log(LogLevel level, string message, params object[] args)
        {
            TestContext.WriteLine($"[{level}] {message}", args);
        }

        public void Verbose(string message, params object[] args)
        {
            Log(LogLevel.Verbose, message, args);
        }

        public void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, args);
        }

        public void Info(string message, params object[] args)
        {
            Log(LogLevel.Info, message, args);
        }

        public void Warn(string message, params object[] args)
        {
            Log(LogLevel.Warn, message, args);
        }

        public void Error(string message, params object[] args)
        {
            Log(LogLevel.Error, message, args);
        }
    }
}
