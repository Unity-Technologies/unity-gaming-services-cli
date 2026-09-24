using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;

namespace Unity.Services.Cli.Scheduler.Deploy;

class SchedulerDeploymentService : SchedulerDeployFetchBase, IDeploymentService
{
    readonly ISchedulerDeploymentHandler m_DeploymentHandler;
    readonly ISchedulerClient m_Client;

    public SchedulerDeploymentService(
        ISchedulerDeploymentHandler deploymentHandler,
        ISchedulerClient client,
        ISchedulerResourceLoader loader)
        : base(loader)
    {
        m_DeploymentHandler = deploymentHandler;
        m_Client = client;
    }

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<AuthoringFile> authoringFiles,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {ServiceType} files...");
        var (loadedFiles, failedToDeserialize) = await GetResourcesFromFiles(authoringFiles.ToPaths(), cancellationToken);

        loadingContext?.Status($"Deploying {ServiceType} files...");
        var res = await m_DeploymentHandler.DeployAsync(
            loadedFiles,
            dryRun: deployInput.DryRun,
            reconcile: deployInput.Reconcile,
            token: cancellationToken);

        IReadOnlyList<IDeploymentItem> authored = res.Deployed
            .Cast<IDeploymentItem>()
            .Concat(failedToDeserialize)
            .ToList();

        var resultItems = SchedulerResultBuilder.Create(authored, fetch: false);

        return new ScheduleDeploymentResult(
            resultItems.Updated,
            resultItems.Deleted,
            resultItems.Created,
            resultItems.Authored,
            resultItems.Failed,
            deployInput.DryRun);
    }
}
