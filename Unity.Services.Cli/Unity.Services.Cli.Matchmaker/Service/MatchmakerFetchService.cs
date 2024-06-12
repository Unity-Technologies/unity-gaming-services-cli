using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Matchmaker.Authoring.Core.Fetch;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.Matchmaker.Service;

class MatchmakerFetchService : IFetchService
{
    readonly IMatchmakerFetchHandler m_FetchHandler;
    readonly IConfigApiClient m_Client;

    public MatchmakerFetchService(IConfigApiClient client, IMatchmakerFetchHandler fetchHandler)
    {
        m_Client = client;
        m_FetchHandler = fetchHandler;
    }

    public string ServiceType => "Matchmaker";
    public string ServiceName => "matchmaker";
    public IReadOnlyList<string> FileExtensions
    {
        get => new[] { ".mme", ".mmq" };
    }

    public async Task<FetchResult> FetchAsync(
        FetchInput input,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(projectId, environmentId, cancellationToken);
        loadingContext?.Status($"Fetching {ServiceType} files...");
        if (File.Exists(input.Path))
        {
            throw new MatchmakerException("The provided path is not a directory.");
        }

        var res = await m_FetchHandler.FetchAsync(input.Path, filePaths, input.Reconcile, input.DryRun, cancellationToken);

        if (!string.IsNullOrEmpty(res.AbortMessage))
            throw new MatchmakerException(res.AbortMessage);

        return new FetchResult(res.Updated, res.Deleted, res.Created, res.Authored, res.Failed, input.DryRun);
    }
}

