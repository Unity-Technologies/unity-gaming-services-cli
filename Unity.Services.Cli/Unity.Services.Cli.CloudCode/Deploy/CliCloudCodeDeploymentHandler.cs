using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Logging;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CliCloudCodeDeploymentHandler<TClient> : CloudCodeDeploymentHandler
    where TClient : ICloudCodeClient
{
    public CliCloudCodeDeploymentHandler(
        TClient client,
        IDeploymentAnalytics deploymentAnalytics,
        ILogger logger,
        IPreDeployValidator preDeployValidator)
        :
        base(client, deploymentAnalytics, logger, preDeployValidator)
    { }

    protected override void UpdateScriptStatus(
        IScript script, string message, string detail, StatusSeverityLevel level = StatusSeverityLevel.None)
    {
        if (script is DeployContent deployContent)
        {
            deployContent.Status = new DeploymentStatus(
                message,
                detail,
                (SeverityLevel)Enum.Parse(typeof(SeverityLevel), level.ToString()));
        }
        else if (script is ModuleDeployContent moduleDeployContent)
        {
            moduleDeployContent.Status = new DeploymentStatus(
                message,
                detail,
                (SeverityLevel)Enum.Parse(typeof(SeverityLevel), level.ToString()));
        }
    }

    protected override void UpdateScriptProgress(IScript script, float progress)
    {
        if (script is DeployContent deployContent)
        {
            deployContent.Progress = progress;
        }
        else if (script is ModuleDeployContent modDeployContent)
        {
            modDeployContent.Progress = progress;
        }
    }
}
