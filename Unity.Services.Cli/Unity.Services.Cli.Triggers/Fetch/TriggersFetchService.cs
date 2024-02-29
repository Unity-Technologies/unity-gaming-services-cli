using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Cli.Triggers.IO;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.Fetch;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.Triggers.Fetch;

class TriggersFetchService : TriggerDeployFetchBase, IFetchService
{
    readonly ITriggersFetchHandler m_FetchHandler;
    readonly ITriggersClient m_Client;

    public TriggersFetchService(
        ITriggersFetchHandler fetchHandler,
        ITriggersClient client,
        ITriggersResourceLoader resourceLoader) : base(resourceLoader)
    {
        m_FetchHandler = fetchHandler;
        m_Client = client;
    }

    public string ServiceType => TriggersConstants.ServiceType;
    public string ServiceName => TriggersConstants.ServiceName;

    public IReadOnlyList<string> FileExtensions => new[]
    {
        TriggersConstants.DeployFileExtension
    };

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {ServiceType} files...");
        var (deserializedFiles, failedToDeserialize) = await GetResourcesFromFiles(filePaths, cancellationToken);

        loadingContext?.Status($"Fetching {ServiceType} files...");
        var res = await m_FetchHandler.FetchAsync(
            input.Path,
            deserializedFiles.SelectMany(f => f.Content.Configs).ToList(),
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        SetFileStatus(deserializedFiles);

        var failedToDeploy = deserializedFiles
            .Where(f => f.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();

        var createdFiles = res.Created.Select(
            t => new TriggersFileItem(
                new TriggersConfigFile(
                    new List<TriggerConfig>()
                    {
                        (TriggerConfig)t
                    }),
                t.Path, 100, Statuses.Deployed));

        return new TriggersFetchResult(
            res.Updated,
            res.Deleted,
            res.Created,
            deserializedFiles
                .Where(f => f.Status.MessageSeverity != SeverityLevel.Error)
                .Concat(createdFiles)
                .ToList(),
            failedToDeserialize
                .Cast<IDeploymentItem>()
                .Concat(failedToDeploy)
                .ToList(),
            input.DryRun);
    }
}
