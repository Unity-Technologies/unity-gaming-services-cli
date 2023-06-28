using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest;

class LogMessageTestHelper : IDisposable
{
    readonly StringWriter m_StringWriter;

    readonly TextWriter m_PreviousTextWriter;
    readonly TextWriter m_PreviousErrorTextWriter;

    public string LogMessage => m_StringWriter.ToString();

    public LogMessageTestHelper()
    {
        m_PreviousTextWriter = System.Console.Out;
        m_PreviousErrorTextWriter = System.Console.Error;
        m_StringWriter = new StringWriter();
        System.Console.SetOut(m_StringWriter);
        System.Console.SetError(m_StringWriter);
    }

    public void Dispose()
    {
        System.Console.SetOut(m_PreviousTextWriter);
        System.Console.SetError(m_PreviousErrorTextWriter);
        m_StringWriter.Close();
    }

    public static string GetJsonLogFormatted(object? result, List<LogMessage> logMessages)
    {
        return GetJsonResult(result) + GetJsonLogMessage(logMessages);
    }

    static string GetJsonResult(object? result)
    {
        return JsonConvert.SerializeObject(result, Formatting.Indented) + System.Environment.NewLine;
    }

    static string GetJsonLogMessage(List<LogMessage> logMessages)
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

        string result = "";
        if (messages.Any())
        {
            result = JsonConvert.SerializeObject(messages, Formatting.Indented) + System.Environment.NewLine;

        }

        return result;
    }
}
