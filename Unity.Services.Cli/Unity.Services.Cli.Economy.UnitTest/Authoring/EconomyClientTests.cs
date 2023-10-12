using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Economy.Authoring;
using Unity.Services.Cli.Economy.Handlers;
using Unity.Services.Cli.Economy.Service;
using Unity.Services.Economy.Editor.Authoring.Core.IO;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.UnitTest.Authoring;

[TestFixture]
public class EconomyClientTests
{
    const string k_TestProjectId = "00000000-0000-0000-0000-000000000000";
    const string k_TestEnvironmentId = "00000000-0000-0000-0000-000000000000";

    readonly Mock<IEconomyService> m_MockEconomyService = new Mock<IEconomyService>();
    readonly Mock<IFileSystem> m_MockFileSystem = new Mock<IFileSystem>();

    readonly EconomyResource m_TestEconomyResource;
    readonly EconomyClient m_EconomyClient;

    readonly CurrencyItemResponse m_TestCurrencyItemResponse = new CurrencyItemResponse(
        "TEST_ID",
        "TEST_NAME",
        CurrencyItemResponse.TypeEnum.CURRENCY,
        0,
        100,
        "custom data",
        new ModifiedMetadata(DateTime.Now),
        new ModifiedMetadata(DateTime.Now));

    readonly InventoryItemResponse m_TestInventoryItemResponse = new InventoryItemResponse(
        "TEST_ID",
        "TEST_NAME",
        InventoryItemResponse.TypeEnum.INVENTORYITEM,
        "custom data",
        new ModifiedMetadata(DateTime.Now),
        new ModifiedMetadata(DateTime.Now));

    readonly VirtualPurchaseResourceResponse m_TestVirtualPurchaseResponse = new VirtualPurchaseResourceResponse(
        "TEST_ID",
        "TEST_NAME",
        VirtualPurchaseResourceResponse.TypeEnum.VIRTUALPURCHASE,
        new List<VirtualPurchaseResourceResponseCostsInner>(),
        new List<VirtualPurchaseResourceResponseRewardsInner>(),
        "custom data",
        new ModifiedMetadata(DateTime.Now),
        new ModifiedMetadata(DateTime.Now));

    readonly RealMoneyPurchaseResourceResponse m_TestRealMoneyPurchaseResponse = new RealMoneyPurchaseResourceResponse(
        "TEST_ID",
        "TEST_NAME",
        RealMoneyPurchaseResourceResponse.TypeEnum.MONEYPURCHASE,
        new RealMoneyPurchaseItemResponseStoreIdentifiers(),
        new List<RealMoneyPurchaseResourceResponseRewardsInner>(),
        "custom data",
        new ModifiedMetadata(DateTime.Now),
        new ModifiedMetadata(DateTime.Now));

    public EconomyClientTests()
    {
        m_EconomyClient = new EconomyClient(
            m_MockEconomyService.Object,
            k_TestProjectId,
            k_TestEnvironmentId,
            CancellationToken.None);

        m_TestEconomyResource = new EconomyCurrency(m_TestCurrencyItemResponse.Id)
        {
            Name = m_TestCurrencyItemResponse.Name
        };
    }

    [SetUp]
    public void SetUp()
    {
        m_MockEconomyService.Reset();
        m_MockFileSystem.Reset();

        m_MockFileSystem
            .Setup(x =>
                x.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(m_TestCurrencyItemResponse.ToJson);
    }

    [Test]
    public void ConstructorInitializeProperties()
    {
        CancellationToken cancellationToken = new(true);
        var economyClient = new EconomyClient(
            m_MockEconomyService.Object,
            "k_TestProjectId",
            "k_TestEnvironmentId",
            cancellationToken);

        Assert.Multiple(() =>
        {
            Assert.That(economyClient.ProjectId, Is.EqualTo("k_TestProjectId"));
            Assert.That(economyClient.EnvironmentId, Is.EqualTo("k_TestEnvironmentId"));
            Assert.That(economyClient.CancellationToken, Is.EqualTo(cancellationToken));
        });
    }

    [Test]
    public void InitializeChangeProperties()
    {
        CancellationToken cancellationToken = new(true);
        m_EconomyClient.Initialize(k_TestEnvironmentId, k_TestProjectId, cancellationToken);
        Assert.Multiple(() =>
        {
            Assert.That(m_EconomyClient.ProjectId, Is.SameAs(k_TestProjectId));
            Assert.That(m_EconomyClient.EnvironmentId, Is.SameAs(k_TestEnvironmentId));
            Assert.That(m_EconomyClient.CancellationToken, Is.EqualTo(cancellationToken));
        });
    }

    [Test]
    public async Task Edit_CallsEditAsyncCorrectly()
    {
        var expectedResource =
            EconomyConfigurationBuilder.ConstructAddConfigResourceRequest(m_TestEconomyResource);

        await m_EconomyClient.Update(m_TestEconomyResource);

        m_MockEconomyService.Verify(s =>
            s.EditAsync(
                m_TestEconomyResource.Id,
                expectedResource!,
                k_TestProjectId,
                k_TestEnvironmentId,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task Add_CallsAddAsyncCorrectly()
    {
        var expectedResource =
            EconomyConfigurationBuilder.ConstructAddConfigResourceRequest(m_TestEconomyResource);

        await m_EconomyClient.Create(m_TestEconomyResource);

        m_MockEconomyService.Verify(s =>
            s.AddAsync(
                expectedResource!,
                k_TestProjectId,
                k_TestEnvironmentId,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task Delete_CallsDeleteAsyncCorrectly()
    {
        var testId = "TEST_ID";
        await m_EconomyClient.Delete(testId);

        m_MockEconomyService.Verify(s =>
                s.DeleteAsync(
                    testId,
                    k_TestProjectId,
                    k_TestEnvironmentId,
                    CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task Publish_CallsPublishAsyncCorrectly()
    {
        await m_EconomyClient.Publish();

        m_MockEconomyService.Verify(s =>
                s.PublishAsync(
                    k_TestProjectId,
                    k_TestEnvironmentId,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetResources_CallsGetResourcesAsyncCorrectly()
    {
        GetResourcesResponseResultsInner getResResultsInner = new(m_TestCurrencyItemResponse);

        m_MockEconomyService
            .Setup(x =>
                x.GetResourcesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<GetResourcesResponseResultsInner>
                {
                    getResResultsInner
                });

        var resources = await m_EconomyClient.List();

        m_MockEconomyService.Verify(s =>
                s.GetResourcesAsync(
                    k_TestProjectId,
                    k_TestEnvironmentId,
                    CancellationToken.None),
            Times.Once);
        var expectedResource = new EconomyCurrency(m_TestCurrencyItemResponse.Id)
        {
            Name = m_TestCurrencyItemResponse.Name
        };

        Assert.That(resources.Count, Is.EqualTo(1));
        Assert.That(resources[0], Is.EqualTo(expectedResource));
    }

    [Test]
    public async Task ListResources_CallConstructResourcesAsyncCorrectly()
    {
        var responses = new List<GetResourcesResponseResultsInner>()
        {
            new GetResourcesResponseResultsInner(m_TestCurrencyItemResponse),
            new GetResourcesResponseResultsInner(m_TestInventoryItemResponse),
            new GetResourcesResponseResultsInner(m_TestVirtualPurchaseResponse),
            new GetResourcesResponseResultsInner(m_TestRealMoneyPurchaseResponse)
        };

        m_MockEconomyService
            .Setup(x =>
                x.GetResourcesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(responses);

        var resources = await m_EconomyClient.List();

        var expectedResources = new List<IEconomyResource>()
        {
            new EconomyCurrency(m_TestCurrencyItemResponse.Id)
            {
                Name = m_TestCurrencyItemResponse.Name
            },
            new EconomyInventoryItem(m_TestInventoryItemResponse.Id)
            {
                Name = m_TestCurrencyItemResponse.Name
            },
            new EconomyVirtualPurchase(m_TestVirtualPurchaseResponse.Id)
            {
                Name = m_TestVirtualPurchaseResponse.Name
            },
            new EconomyRealMoneyPurchase(m_TestRealMoneyPurchaseResponse.Id)
            {
                Name = m_TestRealMoneyPurchaseResponse.Name
            }
        };

        Assert.That(resources.Count, Is.EqualTo(4));
        Assert.Multiple(
            () =>
            {
                for (int i = 0; i < resources.Count; i++)
                {
                    Assert.That(resources[i], Is.TypeOf(expectedResources[i].GetType()));
                    Assert.That(resources[i], Is.EqualTo(expectedResources[i]));
                }
            });
    }
}
