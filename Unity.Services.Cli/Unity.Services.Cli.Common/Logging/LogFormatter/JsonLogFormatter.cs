using System;
using Microsoft.Extensions.Logging;
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
        m_StdOut.WriteLine(JsonConvert.SerializeObject(logCache.Result, Formatting.Indented));
        WriteMessages(logCache.Messages);
    }

    void WriteMessages(List<LogMessage> logMessages)
    {
        var messages = new List<JsonLogMessage>(logMessages.Capacity);
        foreach (var message in logMessages)
        {
            messages.Add(new JsonLogMessage()
            {
                Message = message.Message,
                Type = message.Type.ToString()
            });
        }

        if (messages.Any())
        {
            m_StdErr.WriteLine(JsonConvert.SerializeObject(messages, Formatting.Indented));
        }
    }
}
