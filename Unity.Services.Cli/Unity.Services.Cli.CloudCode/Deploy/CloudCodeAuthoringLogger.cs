using Microsoft.Extensions.Logging;
using ICloudCodeAuthoringLogger = Unity.Services.CloudCode.Authoring.Editor.Core.Logging.ILogger;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeAuthoringLogger : ICloudCodeAuthoringLogger
{
    readonly ILogger m_Logger;

    public CloudCodeAuthoringLogger(ILogger logger)
    {
        m_Logger = logger;
    }

    public void LogError(object message)
    {
        m_Logger.LogError("{Message}", message.ToString());
    }

    public void LogWarning(object message)
    {
        m_Logger.LogWarning("{Message}", message.ToString());
    }

    public void LogInfo(object message)
    {
        m_Logger.LogInformation("{Message}", message.ToString());
    }

    public void LogVerbose(object message)
    {

    }
}
