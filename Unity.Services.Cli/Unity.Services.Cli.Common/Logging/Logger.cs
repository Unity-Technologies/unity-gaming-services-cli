using Microsoft.Extensions.Logging;

namespace Unity.Services.Cli.Common.Logging;

public class Logger : ILogger
{
    readonly string m_Name;

    public LogConfiguration Configuration { get; set; }

    readonly LogCache m_Cache = new();

    IDisposable ILogger.BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel)
    {
        return Configuration.LogLevels.ContainsKey(logLevel) &&
            // If on quiet mode it only accepts log level >= of error level
            (!Configuration.IsQuiet || (Configuration.IsQuiet && logLevel >= LogLevel.Error));
    }

    public Logger() : this("", new LogConfiguration()) { }
    public Logger(string name, LogConfiguration logConfiguration)
    {
        (m_Name, Configuration) = (name, logConfiguration);
    }

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (IsResultEvent(eventId))
        {
            m_Cache.AddResult(state);
        }
        else
        {
            m_Cache.AddMessage(state?.ToString(), logLevel, m_Name);
        }
    }

    static bool IsResultEvent(EventId eventId)
    {
        return eventId.Id == LoggerExtension.ResultEventId.Id && eventId.Name == LoggerExtension.ResultEventId.Name;
    }

    public void Write()
    {
        ILogFormatter formatter = Configuration.IsJson ? new JsonLogFormatter() : new LogFormatter(Configuration);
        WriteLog(formatter);
    }

    void WriteLog(ILogFormatter formatter)
    {
        if (!m_Cache.HasLoggedMessage())
            return;

        formatter.WriteLog(m_Cache);
        m_Cache.CleanLog();
    }
}
