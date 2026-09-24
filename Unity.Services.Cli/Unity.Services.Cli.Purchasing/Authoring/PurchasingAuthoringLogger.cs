using Microsoft.Extensions.Logging;
using CoreLogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger;

namespace Unity.Services.Cli.Purchasing.Authoring;

class PurchasingAuthoringLogger : CoreLogger.ILogger
{
    readonly ILogger m_Logger;

    public PurchasingAuthoringLogger(ILogger logger)
    {
        m_Logger = logger;
    }

    public void LogError(object message)
    {
        m_Logger.LogError("{Message}", message is Exception exception ? exception.Message : message);
    }

    public void LogWarning(object message)
    {
        m_Logger.LogWarning("{Message}", message);
    }

    public void LogInfo(object message)
    {
        m_Logger.LogInformation("{Message}", message);
    }

    public void LogVerbose(object message)
    {
        m_Logger.LogDebug("{Message}", message);
    }
}
