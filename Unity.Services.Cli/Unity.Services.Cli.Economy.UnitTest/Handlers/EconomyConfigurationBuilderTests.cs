using NUnit.Framework;
using Unity.Services.Cli.Economy.Handlers;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.UnitTest.Handlers;

public class EconomyConfigurationBuilderTests
{
    IEconomyResource? m_EconomyCurrencyResource;
    IEconomyResource? m_EconomyInventoryItemResource;
    IEconomyResource? m_EconomyVirtualPurchaseResource;
    IEconomyResource? m_EconomyRealMoneyPurchaseResource;

    [SetUp]
    public void Setup()
    {
        m_EconomyCurrencyResource = new EconomyCurrency("NEW_CURRENCY")
        {
            Name = "New Currency",
            Initial = 10,
            Max = 1000,
        };

        m_EconomyInventoryItemResource = new EconomyInventoryItem("SHIELD")
        {
            Name = "Shield"
        };

        m_EconomyVirtualPurchaseResource = new EconomyVirtualPurchase("VIRTUAL_PURCHASE")
        {
            Name = "Virtual Purchase",
            Costs = new[]
            {
                new Cost()
                {
                    ResourceId = "SILVER",
                    Amount = 10
                }
            },
            Rewards = new[]
            {
                new Reward()
                {
                    ResourceId = "GOLD",
                    Amount = 1
                }
            }
        };

        m_EconomyRealMoneyPurchaseResource = new EconomyRealMoneyPurchase("APPLE_PURCHASE")
        {
            Name = "Apple Purchase",
            StoreIdentifiers = new StoreIdentifiers()
            {
                AppleAppStore = "test",
                GooglePlayStore = "google_test"
            },
            Rewards = new[]
            {
                new RealMoneyReward()
                {
                    ResourceId = "SWORD",
                    Amount = 1
                }
            }
        };
    }

    [Test]
    public void EconomyConfigurationBuilder_CreatesValidConfigRequest_ForCurrencyResource()
    {
        var currencyResourceRequest = EconomyConfigurationBuilder
            .ConstructAddConfigResourceRequest(m_EconomyCurrencyResource!)!
            .GetCurrencyItemRequest();

        Assert.AreEqual("NEW_CURRENCY", currencyResourceRequest.Id);
        Assert.AreEqual("New Currency", currencyResourceRequest.Name);
        Assert.AreEqual(CurrencyItemRequest.TypeEnum.CURRENCY, currencyResourceRequest.Type);
        Assert.AreEqual(10, currencyResourceRequest.Initial);
        Assert.AreEqual(1000, currencyResourceRequest.Max);
        Assert.AreEqual(null, currencyResourceRequest.CustomData);
    }

    [Test]
    public void EconomyConfigurationBuilder_CreatesValidConfigRequest_ForInventoryItemResource()
    {
        var inventoryItemRequest = EconomyConfigurationBuilder
            .ConstructAddConfigResourceRequest(m_EconomyInventoryItemResource!)!
            .GetInventoryItemRequest();

        Assert.AreEqual("SHIELD", inventoryItemRequest.Id);
        Assert.AreEqual("Shield", inventoryItemRequest.Name);
        Assert.AreEqual(InventoryItemRequest.TypeEnum.INVENTORYITEM, inventoryItemRequest.Type);
        Assert.AreEqual(null, inventoryItemRequest.CustomData);
    }

    [Test]
    public void EconomyConfigurationBuilder_CreatesValidConfigRequest_ForVirtualPurchaseResource()
    {
        var virtualPurchaseResourceRequest = EconomyConfigurationBuilder
            .ConstructAddConfigResourceRequest(m_EconomyVirtualPurchaseResource!)!
            .GetVirtualPurchaseResourceRequest();

        Assert.AreEqual("VIRTUAL_PURCHASE", virtualPurchaseResourceRequest.Id);
        Assert.AreEqual("Virtual Purchase", virtualPurchaseResourceRequest.Name);
        Assert.AreEqual(VirtualPurchaseResourceRequest.TypeEnum.VIRTUALPURCHASE, virtualPurchaseResourceRequest.Type);
        Assert.AreEqual(10, virtualPurchaseResourceRequest.Costs[0].Amount);
        Assert.AreEqual("SILVER", virtualPurchaseResourceRequest.Costs[0].ResourceId);
        Assert.AreEqual(1, virtualPurchaseResourceRequest.Rewards[0].Amount);
        Assert.AreEqual("GOLD", virtualPurchaseResourceRequest.Rewards[0].ResourceId);
        Assert.AreEqual(null, virtualPurchaseResourceRequest.Rewards[0].DefaultInstanceData);
        Assert.AreEqual(null, virtualPurchaseResourceRequest.CustomData);
    }

    [Test]
    public void EconomyConfigurationBuilder_CreatesValidConfigRequest_ForRealMoneyPurchaseResource()
    {
        var moneyPurchaseResourceRequest = EconomyConfigurationBuilder
            .ConstructAddConfigResourceRequest(m_EconomyRealMoneyPurchaseResource!)!
            .GetRealMoneyPurchaseResourceRequest();

        Assert.AreEqual("APPLE_PURCHASE", moneyPurchaseResourceRequest.Id);
        Assert.AreEqual("Apple Purchase", moneyPurchaseResourceRequest.Name);
        Assert.AreEqual(RealMoneyPurchaseResourceRequest.TypeEnum.MONEYPURCHASE, moneyPurchaseResourceRequest.Type);
        Assert.AreEqual("test", moneyPurchaseResourceRequest.StoreIdentifiers.AppleAppStore);
        Assert.AreEqual("google_test", moneyPurchaseResourceRequest.StoreIdentifiers.GooglePlayStore);
        Assert.AreEqual(1, moneyPurchaseResourceRequest.Rewards[0].Amount);
        Assert.AreEqual("SWORD", moneyPurchaseResourceRequest.Rewards[0].ResourceId);
        Assert.AreEqual(null, moneyPurchaseResourceRequest.CustomData);
    }

}
