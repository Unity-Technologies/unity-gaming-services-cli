using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Economy.Handlers;
using Unity.Services.Cli.Economy.Service;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.Authoring;

class EconomyClient : ICliEconomyClient
{
    const string k_RemoteFilePath = "Remote";
    const string k_ResourceNonMatchTypeError = "Error - resource does not match any valid economy resource types";

    readonly IEconomyService m_EconomyServiceClient;
    internal string ProjectId { get; set; }
    internal string EnvironmentId { get; set; }
    internal CancellationToken CancellationToken { get; set; }

    public EconomyClient(
        IEconomyService service,
        string projectId = "",
        string environmentId = "",
        CancellationToken cancellationToken = default)
    {
        m_EconomyServiceClient = service;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
    }

    public void Initialize(string environmentId, string projectId, CancellationToken cancellationToken)
    {
        EnvironmentId = environmentId;
        ProjectId = projectId;
        CancellationToken = cancellationToken;
    }

    public async Task Update(IEconomyResource economyResource, CancellationToken token = new())
    {
        var configRequest = EconomyConfigurationBuilder.ConstructAddConfigResourceRequest(economyResource);

        await m_EconomyServiceClient.EditAsync(
            economyResource.Id,
            configRequest!,
            ProjectId,
            EnvironmentId,
            token);
    }

    public async Task Create(IEconomyResource economyResource, CancellationToken token = new())
    {
        var configRequest = EconomyConfigurationBuilder.ConstructAddConfigResourceRequest(economyResource);

        await m_EconomyServiceClient.AddAsync(configRequest!, ProjectId, EnvironmentId, token);
    }

    public async Task Delete(string resourceId, CancellationToken token = new())
    {
        await m_EconomyServiceClient.DeleteAsync(resourceId, ProjectId, EnvironmentId, token);
    }

    public async Task<List<IEconomyResource>> List(CancellationToken token = new())
    {
        var results =
            await m_EconomyServiceClient.GetResourcesAsync(ProjectId, EnvironmentId, token);

        return results
            .Select(ConstructResource)
            .ToList();
    }

    public async Task Publish(CancellationToken token = new())
    {
        await m_EconomyServiceClient.PublishAsync(ProjectId, EnvironmentId, token);
    }

    static IEconomyResource ConstructResource(GetResourcesResponseResultsInner result)
    {
        try
        {
            switch (result.ActualInstance)
            {
                case CurrencyItemResponse currencyItemResponse:
                    return ConstructEconomyCurrencyResource(currencyItemResponse);
                case InventoryItemResponse inventoryItemResponse:
                    return ConstructEconomyInventoryItemResource(inventoryItemResponse);
                case VirtualPurchaseResourceResponse virtualPurchaseResourceResponse:
                    return ConstructEconomyVirtualPurchaseResource(virtualPurchaseResourceResponse);
                case RealMoneyPurchaseResourceResponse realMoneyPurchaseResourceResponse:
                    return ConstructEconomyRealMoneyPurchaseResource(realMoneyPurchaseResourceResponse);
                default:
                    throw new JsonSerializationException(k_ResourceNonMatchTypeError);
            }
        }
        catch (JsonSerializationException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
        catch (JsonReaderException e)
        {
            throw new CliException(e.Message, ExitCode.HandledError);
        }
    }

    static IEconomyResource ConstructEconomyInventoryItemResource(InventoryItemResponse resource)
    {
        return new EconomyInventoryItem(resource.Id)
        {
            Name = resource.Name,
            CustomData = resource.CustomData,
            Path = k_RemoteFilePath
        };
    }

    static IEconomyResource ConstructEconomyRealMoneyPurchaseResource(RealMoneyPurchaseResourceResponse resource)
    {
        return new EconomyRealMoneyPurchase(resource.Id)
        {
            Name = resource.Name,
            Rewards = resource.Rewards
                .Select(
                    reward => new RealMoneyReward
                    {
                        Amount = reward.Amount,
                        ResourceId = reward.ResourceId
                    })
                .ToArray(),
            StoreIdentifiers = new StoreIdentifiers()
            {
                AppleAppStore = resource.StoreIdentifiers.AppleAppStore,
                GooglePlayStore = resource.StoreIdentifiers.GooglePlayStore
            },
            CustomData = resource.CustomData,
            Path = k_RemoteFilePath
        };
    }

    static IEconomyResource ConstructEconomyVirtualPurchaseResource(VirtualPurchaseResourceResponse resource)
    {
        return new EconomyVirtualPurchase(resource.Id)
        {
            Name = resource.Name,
            Costs = resource.Costs
                .Select(
                    cost => new Cost
                    {
                        Amount = cost.Amount,
                        ResourceId = cost.ResourceId
                    })
                .ToArray(),
            Rewards = resource.Rewards
                .Select(
                    reward => new Reward
                    {
                        Amount = reward.Amount,
                        ResourceId = reward.ResourceId
                    })
                .ToArray(),
            CustomData = resource.CustomData,
            Path = k_RemoteFilePath
        };
    }

    static IEconomyResource ConstructEconomyCurrencyResource(CurrencyItemResponse resource)
    {
        return new EconomyCurrency(resource.Id)
        {
            Name = resource.Name,
            Initial = resource.Initial,
            Max = resource.Max,
            CustomData = resource.CustomData,
            Path = k_RemoteFilePath
        };
    }
}
