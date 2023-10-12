using Unity.Services.Cli.Economy.Model;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;
using RealMoneyReward = Unity.Services.Economy.Editor.Authoring.Core.Model.RealMoneyReward;

namespace Unity.Services.Cli.Economy.Handlers;

static class EconomyConfigurationBuilder
{
    internal static AddConfigResourceRequest? ConstructAddConfigResourceRequest(IEconomyResource resource)
    {
        var baseResource = new Resource(
            resource.Id,
            resource.Name,
            ((EconomyResource)resource).EconomyType,
            ((EconomyResource)resource).CustomData);

        switch (resource)
        {
            case EconomyCurrency economyCurrency:
                return AddConfigCurrencyResourceRequest(baseResource, economyCurrency);
            case EconomyInventoryItem:
                return AddConfigInventoryItemResourceRequest(baseResource);
            case EconomyVirtualPurchase economyVirtualPurchase:
                return AddConfigVirtualPurchaseResourceRequest(baseResource, economyVirtualPurchase);
            case EconomyRealMoneyPurchase economyMoneyPurchase:
                return AddConfigRealMoneyPurchaseResourceRequest(baseResource, economyMoneyPurchase);
            default:
                return null;
        }
    }
    static AddConfigResourceRequest? AddConfigRealMoneyPurchaseResourceRequest(Resource baseResource, EconomyRealMoneyPurchase economyMoneyPurchase)
    {
        var realMoneyRequest = new RealMoneyPurchaseResourceRequest(
            baseResource.Id,
            baseResource.Name,
            RealMoneyPurchaseResourceRequest.TypeEnum.MONEYPURCHASE,
            new RealMoneyPurchaseItemRequestStoreIdentifiers(
                economyMoneyPurchase.StoreIdentifiers.AppleAppStore,
                economyMoneyPurchase.StoreIdentifiers.GooglePlayStore),
            GetRealMoneyPurchaseRewards(economyMoneyPurchase.Rewards),
            baseResource.CustomData);
        return new AddConfigResourceRequest(realMoneyRequest);
    }

    static AddConfigResourceRequest? AddConfigVirtualPurchaseResourceRequest(Resource baseResource, EconomyVirtualPurchase economyVirtualPurchase)
    {
        var virtualPurchaseRequest = new VirtualPurchaseResourceRequest(
            baseResource.Id,
            baseResource.Name,
            VirtualPurchaseResourceRequest.TypeEnum.VIRTUALPURCHASE,
            GetVirtualPurchaseCosts(economyVirtualPurchase.Costs),
            GetVirtualPurchaseRewards(economyVirtualPurchase.Rewards),
            baseResource.CustomData);
        return new AddConfigResourceRequest(virtualPurchaseRequest);
    }

    static AddConfigResourceRequest? AddConfigInventoryItemResourceRequest(Resource baseResource)
    {
        var inventoryItemRequest = new InventoryItemRequest(
            baseResource.Id,
            baseResource.Name,
            InventoryItemRequest.TypeEnum.INVENTORYITEM,
            baseResource.CustomData);
        return new AddConfigResourceRequest(inventoryItemRequest);
    }

    static AddConfigResourceRequest? AddConfigCurrencyResourceRequest(Resource baseResource, EconomyCurrency economyCurrency)
    {
        var currencyRequest = new CurrencyItemRequest(
            baseResource.Id,
            baseResource.Name,
            CurrencyItemRequest.TypeEnum.CURRENCY,
            economyCurrency.Initial,
            economyCurrency.Max ?? 0,
            baseResource.CustomData);
        return new AddConfigResourceRequest(currencyRequest);
    }

    static List<VirtualPurchaseResourceRequestCostsInner> GetVirtualPurchaseCosts(Unity.Services.Economy.Editor.Authoring.Core.Model.Cost[] costs)
    {
        return costs
            .Select(cost => new VirtualPurchaseResourceRequestCostsInner(cost.ResourceId, cost.Amount))
            .ToList();
    }

    static List<VirtualPurchaseResourceRequestRewardsInner> GetVirtualPurchaseRewards(Reward[] rewards)
    {
        return rewards
            .Select(
                reward =>
                    new VirtualPurchaseResourceRequestRewardsInner(reward.ResourceId, reward.Amount, reward.DefaultInstanceData))
            .ToList();
    }

    static List<RealMoneyPurchaseResourceRequestRewardsInner> GetRealMoneyPurchaseRewards(RealMoneyReward[] rewards)
    {
        return rewards
            .Select(reward => new RealMoneyPurchaseResourceRequestRewardsInner(reward.ResourceId, reward.Amount))
            .ToList();
    }
}
