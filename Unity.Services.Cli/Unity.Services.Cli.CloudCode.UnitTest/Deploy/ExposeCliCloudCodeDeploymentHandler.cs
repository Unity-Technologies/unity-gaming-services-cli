using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Logging;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Deploy;

class ExposeCliCloudCodeDeploymentHandler : CliCloudCodeDeploymentHandler<ICloudCodeClient>
{
    public void ExposeUpdateScriptProgress(IScript script, float progress)
    {
        base.UpdateScriptProgress(script, progress);
    }

    public void ExposeUpdateScriptStatus(IScript script, string message, string detail)
    {
        base.UpdateScriptStatus(script, message, detail);
    }

    public ExposeCliCloudCodeDeploymentHandler(
        ICloudCodeClient client,
        IDeploymentAnalytics deploymentAnalytics,
        ILogger logger,
        IPreDeployValidator preDeployValidator)
        : base(client, deploymentAnalytics, logger, preDeployValidator) { }
}
