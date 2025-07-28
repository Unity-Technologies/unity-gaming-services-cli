namespace Unity.Services.GameServerHosting.LocalProxy.Model
{
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 100,
        Info = 200,
        Warn = 300,
        Error = 400,
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message, params object[] args);
        void Verbose(string message, params object[] args);
        void Debug(string message, params object[] args);
        void Info(string message, params object[] args);
        void Warn(string message, params object[] args);
        void Error(string message, params object[] args);
    }
}
