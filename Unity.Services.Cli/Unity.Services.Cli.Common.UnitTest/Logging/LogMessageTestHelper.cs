using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Logging;

namespace Unity.Services.Cli.Common.UnitTest;

class LogMessageTestHelper : IDisposable
{
    readonly StringWriter m_StringWriter;

    readonly TextWriter m_PreviousTextWriter;

    public string LogMessage => m_StringWriter.ToString();

    public LogMessageTestHelper()
    {
        m_PreviousTextWriter = System.Console.Out;
        m_StringWriter = new StringWriter();
        System.Console.SetOut(m_StringWriter);
    }

    public void Dispose()
    {
        System.Console.SetOut(m_PreviousTextWriter);
        m_StringWriter.Close();
    }

    public static string GetJsonLogMessage(List<LogMessage>? messages, object? result)
    {
        var jsonMessages = new List<JsonLogMessage>();

        if (messages != null)
        {
            foreach (var message in messages)
            {
                jsonMessages.Add(new JsonLogMessage()
                {
                    Message = message.Message,
                    Type = message.Type.ToString()
                });
            }
        }

        return JsonConvert.SerializeObject(new
        {
            Result = result,
            Messages = jsonMessages
        }, Formatting.Indented) + System.Environment.NewLine;
    }
}
