using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.Scheduler.Fetch;

class SchedulerFetchService : SchedulerDeployFetchBase, IFetchService
{
    readonly ISchedulerFetchHandler m_FetchHandler;
    readonly ISchedulerClient m_Client;

    public SchedulerFetchService(
        ISchedulerFetchHandler fetchHandler,
        ISchedulerClient client,
        ISchedulerResourceLoader loader)
        : base(loader)
    {
        m_FetchHandler = fetchHandler;
        m_Client = client;
    }

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<AuthoringFile> authoringFiles,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {ServiceType} files...");
        var (loadedFiles, failedFiles) = await GetResourcesFromFiles(authoringFiles.ToPaths(), cancellationToken);

        loadingContext?.Status($"Fetching {ServiceType} files...");
        var res = await m_FetchHandler.FetchAsync(
            input.Path,
            loadedFiles,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        IReadOnlyList<IDeploymentItem> authored = res.Deployed
            .Cast<IDeploymentItem>()
            .Concat(failedFiles)
            .ToList();

        var resultItems = SchedulerResultBuilder.Create(authored, fetch: true);

        return new SchedulesFetchResult(
            resultItems.Updated,
            resultItems.Deleted,
            resultItems.Created,
            resultItems.Authored,
            resultItems.Failed,
            input.DryRun);
    }
}
