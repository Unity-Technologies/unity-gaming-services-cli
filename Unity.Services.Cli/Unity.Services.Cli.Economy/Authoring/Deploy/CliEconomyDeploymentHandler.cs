using Newtonsoft.Json;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Economy.Editor.Authoring.Core.Deploy;
using Unity.Services.Economy.Editor.Authoring.Core.Logging;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Economy.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Economy.Authoring.Deploy;

class CliEconomyDeploymentHandler : EconomyDeploymentHandler
{
    internal CliEconomyDeploymentHandler(
        IEconomyClient client,
        ILogger logger)
        : base(client, logger) { }

    internal override void UpdateResourceProgress(IEconomyResource resource, float progress)
    {
        ((EconomyResource)resource).Progress = progress;
    }

    internal override void HandleException(Exception exception, IEconomyResource resource, DeployResult result)
    {
        result.Failed.Add(resource);
        resource.Status = new DeploymentStatus(
            Statuses.FailedToDeploy,
            exception.InnerException != null ? exception.InnerException.Message : exception.Message,
            SeverityLevel.Error);
    }

    internal override bool IsLocalResourceUpToDateWithRemote(
        IEconomyResource resource,
        List<IEconomyResource> remoteResources)
    {
        var nbEqualResources = remoteResources
            .Where(r => r.Id.Equals(resource.Id))
            .Count(r => JsonConvert.SerializeObject(resource).Equals(JsonConvert.SerializeObject(r)));

        return nbEqualResources > 0;
    }
}
