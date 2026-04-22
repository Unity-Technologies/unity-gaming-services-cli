using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Unity.Services.Cli.Common.Logging;

class JsonLogFormatter : ILogFormatter
{
    readonly TextWriter m_StdOut;
    readonly TextWriter m_StdErr;

    public JsonLogFormatter(TextWriter stdOut, TextWriter stdErr)
    {
        m_StdOut = stdOut;
        m_StdErr = stdErr;
    }

    /// <inheritdoc cref="ILogFormatter.WriteLog"/>
    public void WriteLog(LogCache logCache)
    {
        if (logCache.Result != null)
        {
            m_StdOut.WriteLine(JsonConvert.SerializeObject(logCache.Result, Formatting.Indented));
        }

        foreach (var logCacheMessage in logCache.Messages)
        {
            logCacheMessage.Message = MessageFormatter.TryFormatAsJson(logCacheMessage.Message);
        }

        WriteMessages(logCache.Messages);
    }

    void WriteMessages(List<LogMessage> logMessages)
    {
        var messages = new List<object>(logMessages.Capacity);
        foreach (var message in logMessages)
        {
            var messageObj = new
            {
                Message = TryGetMessageAsObj(message.Message),
                Type = message.Type.ToString()
            };
            messages.Add(messageObj);
        }

        if (messages.Any())
        {
            m_StdErr.WriteLine(JsonConvert.SerializeObject(messages, Formatting.Indented));
        }
    }

    static object? TryGetMessageAsObj(string? message)
    {
        // Try to parse the message as JSON to pretty-print
        if (!string.IsNullOrEmpty(message))
        {
            try
            {
                return JToken.Parse(message);
            }
            catch (Exception)
            {
                return message;
            }
        }

        return message;
    }
}
