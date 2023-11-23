using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Leaderboards.Authoring.Core.Fetch;
using Unity.Services.Leaderboards.Authoring.Core.Service;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.Leaderboards.Deploy;

class LeaderboardFetchService : IFetchService
{
    readonly ILeaderboardsClient m_Client;
    readonly ILeaderboardsFetchHandler m_FetchHandler;
    readonly ILeaderboardsConfigLoader m_LeaderboardsConfigLoader;
    readonly IDeployFileService m_DeployFileService;
    readonly IUnityEnvironment m_UnityEnvironment;
    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly string m_FileExtension;

    public LeaderboardFetchService(
        ILeaderboardsClient client,
        ILeaderboardsFetchHandler fetchHandler,
        ILeaderboardsConfigLoader leaderboardsConfigLoader,
        IDeployFileService deployFileService,
        IUnityEnvironment unityEnvironment)
    {
        m_Client = client;
        m_FetchHandler = fetchHandler;
        m_LeaderboardsConfigLoader = leaderboardsConfigLoader;
        m_DeployFileService = deployFileService;
        m_UnityEnvironment = unityEnvironment;
        m_ServiceType = "Leaderboard";
        m_ServiceName = "leaderboards";
        m_FileExtension = ".lb";
    }

    public string ServiceType => m_ServiceType;
    public string ServiceName => m_ServiceName;
    public IReadOnlyList<string> FileExtensions => new[]
    {
        m_FileExtension
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

        var leaderboards = await m_LeaderboardsConfigLoader
            .LoadConfigsAsync(filePaths, cancellationToken);

        var deployStatusList = await m_FetchHandler.FetchAsync(
            input.Path,
            leaderboards.Loaded,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        return new FetchResult(
            updated: deployStatusList.Updated,
            deleted: deployStatusList.Deleted,
            created: deployStatusList.Created,
            authored: deployStatusList.Fetched,
            failed: deployStatusList.Failed.Concat(leaderboards.Failed).Cast<IDeploymentItem>().ToList());
    }
}
