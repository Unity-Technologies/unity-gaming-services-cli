using Microsoft.Extensions.Logging;

namespace Unity.Services.Cli.Common.Logging;

public class LogConfiguration
{
    public bool IsJson { get; set; }
    public bool IsQuiet { get; set; }

    public Dictionary<LogLevel, ConsoleColor> LogLevels => new()
    {
        [LogLevel.Information] = ConsoleColor.Green,
        [LogLevel.Warning] = ConsoleColor.DarkYellow,
        [LogLevel.Error] = ConsoleColor.DarkRed,
        [LogLevel.Critical] = ConsoleColor.Red
    };
}
