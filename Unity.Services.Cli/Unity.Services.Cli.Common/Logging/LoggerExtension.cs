using Microsoft.Extensions.Logging;

namespace Unity.Services.Cli.Common.Logging;

public static class LoggerExtension
{
    public const string ResultEventName = "Operation Result";

    public static readonly EventId ResultEventId = new(1000, ResultEventName);

    public static void LogResultValue(
        this ILogger logger, object value, Exception? exception = default, params object?[] args)
    {
        logger.Log(LogLevel.Critical, ResultEventId, value, exception, null!);
    }
}
