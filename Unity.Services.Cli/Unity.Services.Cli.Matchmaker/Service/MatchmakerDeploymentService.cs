using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Matchmaker.Authoring.Core.Deploy;
using Unity.Services.Matchmaker.Authoring.Core.Model;

namespace Unity.Services.Cli.Matchmaker.Service;

class MatchmakerDeploymentService : IDeploymentService
{
    readonly IMatchmakerDeployHandler m_DeploymentHandler;
    readonly IConfigApiClient m_Client;
    readonly IGameServerHostingConfigLoader m_GshConfigLoader;

    public MatchmakerDeploymentService(
        IConfigApiClient client,
        IMatchmakerDeployHandler deploymentHandler,
        IGameServerHostingConfigLoader gshConfigLoader)
    {
        m_Client = client;
        m_DeploymentHandler = deploymentHandler;
        m_GshConfigLoader = gshConfigLoader;
    }

    public string ServiceType => "Matchmaker";
    public string ServiceName => "matchmaker";

    public IReadOnlyList<string> FileExtensions
    {
        get => new[] { ".mme", ".mmq" };
    }

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        await m_Client.Initialize(projectId, environmentId, cancellationToken);

        loadingContext?.Status($"Deploying {ServiceType} files...");

        var availableMultiplayConfig = await GetAvailableMultiplayResources(filePaths, deployInput, cancellationToken);

        var res = await m_DeploymentHandler.DeployAsync(
            filePaths,
            availableMultiplayConfig,
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

    Task<MultiplayResources> GetAvailableMultiplayResources(
        IReadOnlyList<string> filePaths,
        DeployInput deployInput,
        CancellationToken ct)
    {
        var remoteMultiplayResources = m_Client.GetRemoteMultiplayResources();
        return Task.FromResult(remoteMultiplayResources);

        /*  GameServerHosting deployment is not yet implemented in CLI so no point including GSH resources when doing a dry-run since they'll have to be deployed first with something else
           var gshFilePaths = filePaths.Where(p => p.EndsWith(GameServerHostingConfigLoader.k_Extension)).ToList();
           var gshMultiplayResources = new Multiplay.Authoring.Core.Assets.MultiplayConfig();
           if (gshFilePaths.Any())
           {
               gshMultiplayResources = await m_GshConfigLoader.LoadAndValidateAsync(gshFilePaths, ct);
           }
            var localMultiplayResources = new MultiplayResources()
           {
               Fleets = gshMultiplayResources.Fleets.Select(
                       f => new MultiplayResources.Fleet()
                       {
                           Name = f.Key.Name,
                           BuildConfigs = f.Value.BuildConfigurations.Select(
                                   bc => new MultiplayResources.Fleet.BuildConfig()
                                   {
                                       Name = bc.Name,
                                   })
                               .ToList(),
                           QosRegions = f.Value.Regions.Select(
                                   qr => new MultiplayResources.Fleet.QosRegion()
                                   {
                                       Name = qr.Key,
                                   })
                               .ToList()
                       })
                   .ToList()
           };
         
        var availableMultiplayConfig = remoteMultiplayResources;

        if (deployInput.DryRun) // If not dry-run, remote is what we get since GSH is deployed before Matchmaker
        {
           if (deployInput.Services.Contains("GameServerHosting"))
           {
               if (deployInput.Reconcile)
               {
                   availableMultiplayConfig = localMultiplayResources;
               }
               else // Merge local and remote multiplay resources
               {
                   foreach (var fleet in localMultiplayResources.Fleets)
                   {
                       var existingFleet = availableMultiplayConfig.Fleets.FirstOrDefault(f => f.Name == fleet.Name);
                       if (existingFleet.Name != null)
                       {
                           availableMultiplayConfig.Fleets.Add(fleet);
                       }
                       else
                       {
                           // Merge QosRegions
                           foreach (var qosRegion in fleet.QosRegions)
                           {
                               if (existingFleet.QosRegions.All(q => q.Name != qosRegion.Name))
                               {
                                   existingFleet.QosRegions.Add(qosRegion);
                               }
                           }

                           // Merge BuildConfigs
                           foreach (var buildConfig in fleet.BuildConfigs)
                           {
                               if (existingFleet.BuildConfigs.All(b => b.Name != buildConfig.Name))
                               {
                                   existingFleet.BuildConfigs.Add(buildConfig);
                               }
                           }
                       }
                   }
               }
           }
        }
        return availableMultiplayConfig;
        }*/
    }
}
