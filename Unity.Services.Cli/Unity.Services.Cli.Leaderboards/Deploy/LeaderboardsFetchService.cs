using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Leaderboards.Authoring.Core.Fetch;
using Unity.Services.Leaderboards.Authoring.Core.Service;
using FetchResult = Unity.Services.Cli.Authoring.Model.FetchResult;

namespace Unity.Services.Cli.Leaderboards.Deploy;

public class LeaderboardFetchService : IFetchService
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

    string IFetchService.ServiceType => m_ServiceType;
    string IFetchService.ServiceName => m_ServiceName;
    string IFetchService.FileExtension => m_FileExtension;
    public async Task<FetchResult> FetchAsync(FetchInput input, IReadOnlyList<string> filePaths, StatusContext? loadingContext, CancellationToken cancellationToken)
    {
        var environmentId = await m_UnityEnvironment.FetchIdentifierAsync(cancellationToken);
        m_Client.Initialize(environmentId, input.CloudProjectId!, cancellationToken);

        var leaderboards = await m_LeaderboardsConfigLoader
            .LoadConfigsAsync(filePaths, cancellationToken);

        var deployStatusList = await m_FetchHandler.FetchAsync(
            input.Path,
            leaderboards,
            input.DryRun,
            input.Reconcile,
            cancellationToken);

        return new FetchResult(
            updated: deployStatusList.Updated,
            deleted: deployStatusList.Deleted,
            created: deployStatusList.Created,
            authored: deployStatusList.Fetched,
            failed: deployStatusList.Failed);
    }
}
