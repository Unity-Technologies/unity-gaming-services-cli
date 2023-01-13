using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Unity.Services.Cli.Common.Logging;

public sealed class LoggerProvider : ILoggerProvider
{
    readonly IDisposable m_OnChangeToken;

    LogConfiguration m_CurrentConfig;

    readonly ConcurrentDictionary<string, ILogger> m_Loggers =
        new(StringComparer.OrdinalIgnoreCase);

    public LoggerProvider(
        IOptionsMonitor<LogConfiguration> config)
    {
        m_CurrentConfig = config.CurrentValue;
        m_OnChangeToken = config.OnChange(updatedConfig => m_CurrentConfig = updatedConfig);
    }

    public ILogger CreateLogger(string categoryName)
    {
        Func<string, ILogger> loggerFactory = name => new Logger(name, m_CurrentConfig);
        return m_Loggers.GetOrAdd(categoryName, loggerFactory);
    }

    public void Dispose()
    {
        m_Loggers.Clear();
        m_OnChangeToken.Dispose();
    }
}
