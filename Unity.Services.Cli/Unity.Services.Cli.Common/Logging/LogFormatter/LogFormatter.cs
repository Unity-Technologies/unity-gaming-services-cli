namespace Unity.Services.Cli.Common.Logging;

class LogFormatter : ILogFormatter
{
    LogConfiguration LogConfiguration { get; set; }

    public LogFormatter() : this(new LogConfiguration()) { }
    public LogFormatter(LogConfiguration logConfiguration)
    {
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
        if (result is IEnumerable<string> valueList)
        {
            resultString = string.Join(Environment.NewLine, valueList);
        }

        System.Console.WriteLine(resultString);
    }

    void WriteMessages(List<LogMessage> messages)
    {
        foreach (var message in messages)
        {
            var previousForegroundColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = LogConfiguration.LogLevels[message.Type];
            System.Console.Write($"[{message.Type}]: ");
            System.Console.ForegroundColor = previousForegroundColor;
            System.Console.WriteLine(message.Name);
            System.Console.WriteLine($"    {message.Message}");
        }
    }
}
