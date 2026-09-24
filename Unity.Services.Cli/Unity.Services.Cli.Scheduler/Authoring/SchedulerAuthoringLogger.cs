using Microsoft.Extensions.Logging;

namespace Unity.Services.Cli.Scheduler.Authoring;

class SchedulerAuthoringLogger : Tooling.Editor.Scheduler.Authoring.Core.Logger.ILogger
{
    readonly ILogger m_Logger;

    public SchedulerAuthoringLogger(ILogger logger)
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
        m_Logger.LogDebug(message.ToString());
    }
}
