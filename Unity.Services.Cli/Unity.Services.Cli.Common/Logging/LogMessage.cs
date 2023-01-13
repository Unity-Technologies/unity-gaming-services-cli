using Microsoft.Extensions.Logging;

namespace Unity.Services.Cli.Common.Logging;

public class LogMessage
{
    public string? Name { get; set; }
    public string? Message { get; set; }
    public LogLevel Type { get; set; }
}
