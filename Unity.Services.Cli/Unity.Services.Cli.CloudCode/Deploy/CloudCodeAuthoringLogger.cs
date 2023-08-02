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
    }

    public void LogWarning(object message)
    {
    }

    public void LogInfo(object message)
    {
    }

    public void LogVerbose(object message)
    {

    }
}
