using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Triggers.IO;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.Deploy;
using Unity.Services.Triggers.Authoring.Core.Service;

namespace Unity.Services.Cli.Triggers.Deploy;

class TriggersDeploymentService : TriggerDeployFetchBase, IDeploymentService
{
    readonly ITriggersDeploymentHandler m_DeploymentHandler;
    readonly ITriggersClient m_Client;
    readonly ITriggersResourceLoader m_ResourceLoader;

    public TriggersDeploymentService(
        ITriggersDeploymentHandler deploymentHandler,
        ITriggersClient client,
        ITriggersResourceLoader resourceLoader)
        :base(resourceLoader)
    {
        m_DeploymentHandler = deploymentHandler;
        m_Client = client;
        m_ResourceLoader = resourceLoader;
    }

    public string ServiceType => TriggersConstants.ServiceType;
    public string ServiceName => TriggersConstants.ServiceName;
    public IReadOnlyList<string> FileExtensions => new[]
    {
        TriggersConstants.DeployFileExtension
    };

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        m_Client.Initialize(environmentId, projectId, cancellationToken);
        loadingContext?.Status($"Reading {ServiceType} files...");
        var (deserializedFiles, failedToDeserialize) = await GetResourcesFromFiles(filePaths, cancellationToken);

        loadingContext?.Status($"Deploying {ServiceType} files...");
        var res = await m_DeploymentHandler.DeployAsync(
            deserializedFiles.ToList().SelectMany(f => f.Content.Configs).ToList(),
            dryRun: deployInput.DryRun,
            reconcile: deployInput.Reconcile, token: cancellationToken);

        SetFileStatus(deserializedFiles);

        var failedToDeploy = deserializedFiles
            .Where(f => f.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();

        return new TriggersDeploymentResult(
            res.Updated,
            res.Deleted,
            res.Created,
            deserializedFiles.Where(f => f.Status.MessageSeverity != SeverityLevel.Error).ToList(),
            failedToDeserialize.Cast<IDeploymentItem>().Concat(failedToDeploy).ToList(),
            deployInput.DryRun);
    }
}
