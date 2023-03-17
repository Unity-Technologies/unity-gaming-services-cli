using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Logging;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

internal class CliCloudCodeDeploymentHandler : CloudCodeDeploymentHandler, IDeploymentHandlerWithOutput
{
    public ICollection<DeployContent> Contents { get; } = new List<DeployContent>();

    public CliCloudCodeDeploymentHandler(
        ICloudCodeClient client,
        IDeploymentAnalytics deploymentAnalytics,
        IScriptCache scriptCache,
        ILogger logger,
        IPreDeployValidator preDeployValidator) :
        base(client, deploymentAnalytics, scriptCache, logger, preDeployValidator)
    {
    }

    protected override void UpdateScriptStatus(IScript script, string message, string detail,
        StatusSeverityLevel level = StatusSeverityLevel.Error)
    {
        var content = Contents.First(c => string.Equals(script.Path, c.Path));
        content.Status = message;
        content.Detail = detail;
    }

    protected override void UpdateScriptProgress(IScript script, float progress)
    {
        var deployContent = Contents.First(c => string.Equals(script.Path, c.Path.ToString()));
        deployContent.Progress = progress;
    }

}
