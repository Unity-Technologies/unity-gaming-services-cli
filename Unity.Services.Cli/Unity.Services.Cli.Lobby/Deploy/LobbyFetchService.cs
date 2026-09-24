using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Lobby.Deploy;

class LobbyFetchService : IFetchService
{
    readonly LobbyFetchHandler m_Handler;

    public LobbyFetchService(LobbyFetchHandler handler)
    {
        m_Handler = handler;
    }

    public string ServiceType => LobbyConstants.ServiceType;
    public string ServiceName => LobbyConstants.ServiceName;

    static readonly string[] k_FileExtensions = { LobbyConstants.FileExtension };
    static readonly IReadOnlyList<IDeploymentItem> k_Empty = Array.Empty<IDeploymentItem>();
    public IReadOnlyList<string> FileExtensions => k_FileExtensions;

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<AuthoringFile> authoringFiles,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        var loFiles = authoringFiles.ToPaths()
            .Where(p => p.EndsWith(LobbyConstants.FileExtension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var extraFailed = loFiles.Skip(1)
            .Select(f => (IDeploymentItem)new LobbyDeploymentItem(f)
            {
                Status = Statuses.GetFailedToFetch(
                    "Only one Lobby configuration file can be fetched at a time")
            })
            .ToList();

        loadingContext?.Status("Fetching remote Lobby configuration...");
        var result = await m_Handler.FetchAsync(
            projectId,
            environmentId,
            loFiles.FirstOrDefault(),
            input.Path,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        if (extraFailed.Count > 0)
        {
            return new FetchResult(
                result.Updated, result.Deleted, result.Created, result.Fetched,
                result.Failed.Concat(extraFailed).ToList(), input.DryRun);
        }

        return result;
    }
}
