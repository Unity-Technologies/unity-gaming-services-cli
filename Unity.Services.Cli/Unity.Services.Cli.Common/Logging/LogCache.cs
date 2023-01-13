using Microsoft.Extensions.Logging;

namespace Unity.Services.Cli.Common.Logging;

public class LogCache
{
    public LogCache()
    {
        Messages = new List<LogMessage>();
    }

    public object? Result { get; set; }

    public List<LogMessage> Messages { get; set; }

    public bool HasLoggedMessage() => Result != null || (Messages.Count > 0);

    public void AddResult(object? result)
    {
        Result = result;
    }

    public void AddMessage(string? message, LogLevel logLevel, string logName = "")
    {
        Messages.Add(new LogMessage()
        {
            Message = message,
            Type = logLevel,
            Name = logName
        });
    }

    public void CleanLog()
    {
        Result = null;
        Messages.Clear();
    }
}
