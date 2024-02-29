using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Scheduler.Authoring.Core.Service;

namespace Unity.Services.Cli.Scheduler.Deploy;

class SchedulerDeploymentService : SchedulerDeployFetchBase, IDeploymentService
{
    readonly IScheduleDeploymentHandler m_DeploymentHandler;
    readonly ISchedulerClient m_Client;

    public SchedulerDeploymentService(
        IScheduleDeploymentHandler deploymentHandler,
        ISchedulerClient client,
        IScheduleResourceLoader loader) : base(loader)
    {
        m_DeploymentHandler = deploymentHandler;
        m_Client = client;
    }

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {ServiceType} files...");
        var (deserializedFiles, failedToDeserialize) = await GetResourcesFromFiles(filePaths, cancellationToken);

        loadingContext?.Status($"Deploying {ServiceType} files...");
        var res = await m_DeploymentHandler.DeployAsync(
            deserializedFiles.ToList().SelectMany(f => f.Content.Configs.Values).ToList(),
            dryRun: deployInput.DryRun,
            reconcile: deployInput.Reconcile, token: cancellationToken);

        SetFileStatus(deserializedFiles);

        var failedToDeploy = deserializedFiles
            .Where(f => f.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();

        return new ScheduleDeploymentResult(
            res.Updated,
            res.Deleted,
            res.Created,
            deserializedFiles.Where(f => f.Status.MessageSeverity != SeverityLevel.Error).ToList(),
            failedToDeserialize.Cast<IDeploymentItem>().Concat(failedToDeploy).ToList(),
            deployInput.DryRun);
    }
}
