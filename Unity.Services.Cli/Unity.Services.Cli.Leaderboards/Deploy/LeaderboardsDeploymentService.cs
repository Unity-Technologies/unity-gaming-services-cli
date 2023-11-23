using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;

namespace Unity.Services.Cli.Leaderboards.Deploy;

class LeaderboardDeploymentService : IDeploymentService
{
    readonly ILeaderboardsClient m_Client;
    readonly ILeaderboardsDeploymentHandler m_DeploymentHandler;
    readonly ILeaderboardsConfigLoader m_LeaderboardsConfigLoader;
    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly string m_DeployFileExtension;

    public LeaderboardDeploymentService(
        ILeaderboardsClient client,
        ILeaderboardsDeploymentHandler deploymentHandler,
        ILeaderboardsConfigLoader leaderboardsConfigLoader)
    {
        m_Client = client;
        m_DeploymentHandler = deploymentHandler;
        m_LeaderboardsConfigLoader = leaderboardsConfigLoader;
        m_ServiceType = "Leaderboard";
        m_ServiceName = "leaderboards";
        m_DeployFileExtension = ".lb";
    }

    public string ServiceType => m_ServiceType;
    public string ServiceName => m_ServiceName;
    public IReadOnlyList<string> FileExtensions => new[]
    {
        m_DeployFileExtension
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

        var files = await m_LeaderboardsConfigLoader.LoadConfigsAsync(filePaths, cancellationToken);

        var deployStatusList = await m_DeploymentHandler.DeployAsync(
            files.Loaded,
            deployInput.DryRun,
            deployInput.Reconcile,
            cancellationToken);

        return new DeploymentResult(
            deployStatusList.Updated,
            deployStatusList.Deleted,
            deployStatusList.Created,
            deployStatusList.Deployed,
            deployStatusList.Failed.Concat(files.Failed).Cast<IDeploymentItem>().ToList());
    }
}
