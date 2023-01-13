using Newtonsoft.Json;

namespace Unity.Services.Cli.Common.Logging;

class JsonLogFormatter : ILogFormatter
{
    /// <inheritdoc cref="ILogFormatter.WriteLog"/>
    public void WriteLog(LogCache logCache)
    {
        var messages = new List<JsonLogMessage>(logCache.Messages.Capacity);
        foreach (var message in logCache.Messages)
        {
            messages.Add(new JsonLogMessage()
            {
                Message = message.Message,
                Type = message.Type.ToString()
            });
        }

        var jsonLine = JsonConvert.SerializeObject(new
        {
            logCache.Result,
            Messages = messages
        }, Formatting.Indented);
        System.Console.WriteLine(jsonLine);
    }
}
