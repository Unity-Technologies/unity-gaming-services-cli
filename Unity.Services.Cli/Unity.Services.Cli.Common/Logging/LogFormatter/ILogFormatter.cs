namespace Unity.Services.Cli.Common.Logging;

/// <remarks>
/// Each log formatter should implement this interface
/// </remarks>
public interface ILogFormatter
{
    /// <summary>
    /// Prints log to the console using the correct format
    /// </summary>
    /// <param name="logCache">cache containing the logs to be printed</param>
    public void WriteLog(LogCache logCache);
}
