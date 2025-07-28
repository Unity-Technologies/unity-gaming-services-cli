namespace Unity.Services.GameServerHosting.LocalProxy.Model
{
    public class NilLogger : ILogger
    {
        public void Log(LogLevel level, string message, params object[] args)
        {
        }

        public void Verbose(string message, params object[] args)
        {
        }

        public void Debug(string message, params object[] args)
        {
        }

        public void Info(string message, params object[] args)
        {
        }

        public void Warn(string message, params object[] args)
        {
        }

        public void Error(string message, params object[] args)
        {
        }
    }
}
