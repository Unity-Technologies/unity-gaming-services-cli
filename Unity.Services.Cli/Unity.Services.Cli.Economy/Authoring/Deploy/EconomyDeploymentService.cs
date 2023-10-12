using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Economy.Editor.Authoring.Core.Deploy;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Client;
using Statuses = Unity.Services.Cli.Authoring.Model.Statuses;

namespace Unity.Services.Cli.Economy.Authoring.Deploy;

class EconomyDeploymentService : IDeploymentService
{
    ICliEconomyClient m_EconomyClient;
    IEconomyResourcesLoader m_EconomyResourcesLoader;
    IEconomyDeploymentHandler m_DeploymentHandler;

    public EconomyDeploymentService(
        ICliEconomyClient economyClient,
        IEconomyResourcesLoader economyResourcesLoader,
        IEconomyDeploymentHandler deploymentHandler)
    {
        m_EconomyClient = economyClient;
        m_EconomyResourcesLoader = economyResourcesLoader;
        m_DeploymentHandler = deploymentHandler;
    }

    public string ServiceType => Constants.ServiceType;
    public string ServiceName => Constants.ServiceName;

    public IReadOnlyList<string> FileExtensions => new[]
    {
        EconomyResourcesExtensions.Currency,
        EconomyResourcesExtensions.InventoryItem,
        EconomyResourcesExtensions.MoneyPurchase,
        EconomyResourcesExtensions.VirtualPurchase
    };

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        DeployResult deployResult = null!;
        m_EconomyClient.Initialize(environmentId, projectId, cancellationToken);
        var reconcile = deployInput.Reconcile;
        var dryRun = deployInput.DryRun;

        var resourceLoadTaskList = filePaths
            .Select(path => m_EconomyResourcesLoader.LoadResourceAsync(path, cancellationToken))
            .ToList();

        await Task.WhenAll(resourceLoadTaskList);

        var resourceList = resourceLoadTaskList
            .Select(task => task.Result)
            .ToList();
        var validResources = resourceList
            .Where(resource => resource.Status.Message != Statuses.FailedToRead)
            .ToList();
        var failedResources = resourceList
            .Where(resource => resource.Status.Message == Statuses.FailedToRead)
            .ToList();

        loadingContext?.Status($"Deploying {Constants.ServiceType} Files...");

        try
        {
            deployResult = await m_DeploymentHandler.DeployAsync(
                validResources,
                dryRun,
                reconcile,
                cancellationToken);
        }
        catch (ApiException)
        {
            // Ignoring it because this exception should already be logged into the deployment content status
        }

        if (deployResult == null || resourceLoadTaskList.Count == 0)
        {
            return new DeploymentResult(resourceList.ToList());
        }

        return new DeploymentResult(
            deployResult.Updated,
            deployResult.Deleted,
            deployResult.Created,
            deployResult.Deployed,
            deployResult.Failed.Concat(failedResources).ToList(),
            dryRun);
    }
}
