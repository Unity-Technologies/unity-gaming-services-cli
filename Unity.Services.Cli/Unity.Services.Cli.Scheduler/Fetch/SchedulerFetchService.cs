using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;
using Statuses = Unity.Services.Scheduler.Authoring.Core.Model.Statuses;

namespace Unity.Services.Cli.Scheduler.Fetch;

class SchedulerFetchService : SchedulerDeployFetchBase, IFetchService
{
    readonly IScheduleFetchHandler m_FetchHandler;
    readonly ISchedulerClient m_Client;

    public SchedulerFetchService(
        IScheduleFetchHandler fetchHandler,
        ISchedulerClient client,
        IScheduleResourceLoader loader) : base(loader)
    {
        m_FetchHandler = fetchHandler;
        m_Client = client;
    }

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {ServiceType} files...");
        var (deserializedFiles, failedToDeserialize) = await GetResourcesFromFiles(filePaths, cancellationToken);

        loadingContext?.Status($"Fetching {ServiceType} files...");
        var res = await m_FetchHandler.FetchAsync(
            input.Path,
            deserializedFiles.SelectMany(f => f.Content.Configs.Values).ToList(),
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        SetFileStatus(deserializedFiles);

        var failedToDeploy = deserializedFiles
            .Where(f => f.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();

        var createdFiles = res.Created.Select(
            t => new ScheduleFileItem(
                new ScheduleConfigFile(
                    new Dictionary<string, ScheduleConfig>()
                    {
                        { t.Name, (ScheduleConfig)t }
                    }),
                t.Path, 100, Statuses.GetDeployed("Created")));

        return new SchedulesFetchResult(
            res.Updated,
            res.Deleted,
            res.Created,
            deserializedFiles.Where(f => f.Status.MessageSeverity != SeverityLevel.Error)
                .Concat(createdFiles).ToList(),
            failedToDeserialize.Cast<IDeploymentItem>().Concat(failedToDeploy).ToList(),
            input.DryRun);
    }
}
