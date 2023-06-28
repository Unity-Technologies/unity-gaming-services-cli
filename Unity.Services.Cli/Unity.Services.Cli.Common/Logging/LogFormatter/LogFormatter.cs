namespace Unity.Services.Cli.Common.Logging;

class LogFormatter : ILogFormatter
{
    readonly TextWriter m_Stdout;
    readonly TextWriter m_StdErr;
    LogConfiguration LogConfiguration { get; set; }

    public LogFormatter(TextWriter stdout, TextWriter stdErr) : this(new LogConfiguration(), stdout, stdErr) { }
    public LogFormatter(LogConfiguration logConfiguration, TextWriter stdout, TextWriter stdErr)
    {
        m_Stdout = stdout;
        m_StdErr = stdErr;
        LogConfiguration = logConfiguration;
    }

    /// <inheritdoc cref="ILogFormatter.WriteLog"/>
    public void WriteLog(LogCache logCache)
    {
        if (logCache.Result != null)
        {
            WriteResult(logCache.Result);
        }

        WriteMessages(logCache.Messages);
    }

    void WriteResult(object result)
    {
        var resultString = result.ToString();
        if (result is IEnumerable<object> valueList)
        {
            resultString = string.Join(Environment.NewLine, valueList);
        }

        m_Stdout.WriteLine(resultString);
    }

    void WriteMessages(List<LogMessage> messages)
    {
        foreach (var message in messages)
        {
            var previousForegroundColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = LogConfiguration.LogLevels[message.Type];
            m_StdErr.Write($"[{message.Type}]: ");
            System.Console.ForegroundColor = previousForegroundColor;
            m_StdErr.WriteLine(message.Name);
            m_StdErr.WriteLine($"    {message.Message}");
        }
    }
}
