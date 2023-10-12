using Microsoft.Extensions.Logging;
namespace Unity.Services.Cli.Economy.Authoring;

class EconomyAuthoringLogger : Services.Economy.Editor.Authoring.Core.Logging.ILogger
{
    readonly ILogger m_Logger;

    public EconomyAuthoringLogger(ILogger logger)
    {
        m_Logger = logger;
    }

    public void LogError(object message)
    {
        m_Logger.LogError(message.ToString());
    }

    public void LogWarning(object message)
    {
        m_Logger.LogWarning(message.ToString());
    }

    public void LogInfo(object message)
    {
        m_Logger.LogInformation(message.ToString());
    }

    public void LogVerbose(object message)
    {
    }
}
