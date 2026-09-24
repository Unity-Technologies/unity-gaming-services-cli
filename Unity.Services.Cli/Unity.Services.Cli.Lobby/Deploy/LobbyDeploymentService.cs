using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Lobby.Deploy;

class LobbyDeploymentService : IDeploymentService
{
    readonly LobbyDeploymentHandler m_Handler;
    readonly ILobbyResourceLoader m_ResourceLoader;

    public LobbyDeploymentService(
        LobbyDeploymentHandler handler,
        ILobbyResourceLoader resourceLoader)
    {
        m_Handler = handler;
        m_ResourceLoader = resourceLoader;
    }

    public string ServiceType => LobbyConstants.ServiceType;
    public string ServiceName => LobbyConstants.ServiceName;

    static readonly string[] k_FileExtensions = { LobbyConstants.FileExtension };
    static readonly IReadOnlyList<IDeploymentItem> k_Empty = Array.Empty<IDeploymentItem>();
    public IReadOnlyList<string> FileExtensions => k_FileExtensions;

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<AuthoringFile> authoringFiles,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        var loFiles = authoringFiles.ToPaths()
            .Where(p => p.EndsWith(LobbyConstants.FileExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        LobbyDeploymentItem? item = null;
        var extraFailed = new List<IDeploymentItem>();

        if (loFiles.Count > 0)
        {
            extraFailed = loFiles.Skip(1)
                .Select(f => (IDeploymentItem)new LobbyDeploymentItem(f)
                {
                    Status = new DeploymentStatus(
                        "Failed to deploy",
                        "Only one Lobby configuration file can be deployed at a time",
                        SeverityLevel.Error)
                })
                .ToList();

            loadingContext?.Status("Reading Lobby configuration...");
            item = await m_ResourceLoader.LoadResource(loFiles[0], cancellationToken);

            if (item.Config == null)
            {
                extraFailed.Add(item);
                return new DeploymentResult(
                    k_Empty, k_Empty, k_Empty, k_Empty, extraFailed, deployInput.DryRun);
            }
        }

        loadingContext?.Status("Deploying Lobby configuration...");
        var result = await m_Handler.DeployAsync(
            item, projectId, environmentId,
            deployInput.DryRun, deployInput.Reconcile, cancellationToken);

        if (extraFailed.Count > 0)
        {
            return new DeploymentResult(
                result.Updated, result.Deleted, result.Created, result.Deployed,
                result.Failed.Concat(extraFailed).ToList(), deployInput.DryRun);
        }

        return result;
    }
}
