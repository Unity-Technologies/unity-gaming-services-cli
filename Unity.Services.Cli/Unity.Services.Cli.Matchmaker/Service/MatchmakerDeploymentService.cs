using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Deploy;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model;

namespace Unity.Services.Cli.Matchmaker.Service;

class MatchmakerDeploymentService : IDeploymentService
{
    readonly IMatchmakerDeployHandler m_DeploymentHandler;
    readonly IConfigApiClient m_Client;

    public MatchmakerDeploymentService(
        IConfigApiClient client,
        IMatchmakerDeployHandler deploymentHandler)
    {
        m_Client = client;
        m_DeploymentHandler = deploymentHandler;
    }

    public string ServiceType => "Matchmaker";
    public string ServiceName => "matchmaker";

    public IReadOnlyList<string> FileExtensions
    {
        get => new[] { ".mme", ".mmq" };
    }

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<AuthoringFile> authoringFiles,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(projectId, environmentId, cancellationToken);

        loadingContext?.Status($"Deploying {ServiceType} files...");

        var remoteMultiplayResources = m_Client.GetRemoteMultiplayResources();

        var res = await m_DeploymentHandler.DeployAsync(
            authoringFiles.ToPaths(),
            remoteMultiplayResources,
            deployInput.Reconcile,
            deployInput.DryRun,
            cancellationToken);

        if (!string.IsNullOrEmpty(res.AbortMessage))
            throw new MatchmakerException(res.AbortMessage);

        return new DeploymentResult(
            res.Updated,
            res.Deleted,
            res.Created,
            res.Authored,
            res.Failed,
            deployInput.DryRun
        );
    }
}
