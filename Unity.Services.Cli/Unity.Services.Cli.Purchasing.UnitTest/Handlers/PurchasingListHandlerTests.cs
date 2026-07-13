using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Purchasing.Handlers;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Purchasing.UnitTest.Handlers;

[TestFixture]
class PurchasingListHandlerTests
{
    const string k_ProjectId = "00000000-0000-0000-0000-000000000000";
    const string k_EnvironmentId = "00000000-0000-0000-0000-000000000001";

    Mock<IUnityEnvironment> m_MockEnvironment = null!;
    Mock<IConsoleTable> m_MockConsoleTable = null!;
    Mock<ILogger> m_MockLogger = null!;
    Mock<ILoadingIndicator> m_MockLoadingIndicator = null!;
    FakeClient m_FakeClient = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockEnvironment = new Mock<IUnityEnvironment>();
        m_MockEnvironment
            .Setup(e => e.FetchIdentifierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(k_EnvironmentId);

        m_MockConsoleTable = new Mock<IConsoleTable>();
        m_MockLogger = new Mock<ILogger>();
        m_MockLoadingIndicator = new Mock<ILoadingIndicator>();
        m_MockLoadingIndicator
            .Setup(l => l.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()))
            .Callback<string, Func<StatusContext?, Task>>((_, action) => action(null));

        m_FakeClient = new FakeClient();
    }

    [Test]
    public async Task ListAsync_CallsLoadingIndicator()
    {
        var input = new CommonInput { CloudProjectId = k_ProjectId };

        await PurchasingListHandler.PurchasingListHandlerHandlerAsync(
            input,
            m_MockEnvironment.Object,
            m_FakeClient,
            m_MockConsoleTable.Object,
            m_MockLogger.Object,
            m_MockLoadingIndicator.Object,
            CancellationToken.None);

        m_MockLoadingIndicator.Verify(
            l => l.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task ListAsync_CallsDrawTable()
    {
        var input = new CommonInput { CloudProjectId = k_ProjectId };

        await PurchasingListHandler.PurchasingListHandlerHandlerAsync(
            input,
            m_MockEnvironment.Object,
            m_FakeClient,
            m_MockConsoleTable.Object,
            m_MockLogger.Object,
            m_MockLoadingIndicator.Object,
            CancellationToken.None);

        m_MockConsoleTable.Verify(t => t.DrawTable(), Times.Once);
    }

    [Test]
    public async Task ListAsync_AddsRowForEachItem()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "sku_1", ProductType = ProductType.Consumable },
            new() { uSku = "sku_2", ProductType = ProductType.NonConsumable },
        };

        var input = new CommonInput { CloudProjectId = k_ProjectId };

        await PurchasingListHandler.PurchasingListHandlerHandlerAsync(
            input,
            m_MockEnvironment.Object,
            m_FakeClient,
            m_MockConsoleTable.Object,
            m_MockLogger.Object,
            m_MockLoadingIndicator.Object,
            CancellationToken.None);

        m_MockConsoleTable.Verify(t => t.AddRow(It.IsAny<Text[]>()), Times.Exactly(2));
    }

    [Test]
    public async Task ListAsync_AddsSubRowsForProductDetailsAndPricing()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new()
            {
                uSku = "sku_1",
                ProductType = ProductType.Consumable,
                ProductDetails = new List<ProductDetails>
                {
                    new() { Title = "My Product", Language = TranslationLocale.en_US },
                },
                PricingDetails = new List<PricingDetails>
                {
                    new() { CurrencyCode = "USD", Amount = 4.99 },
                },
            },
        };

        var input = new CommonInput { CloudProjectId = k_ProjectId };

        await PurchasingListHandler.PurchasingListHandlerHandlerAsync(
            input,
            m_MockEnvironment.Object,
            m_FakeClient,
            m_MockConsoleTable.Object,
            m_MockLogger.Object,
            m_MockLoadingIndicator.Object,
            CancellationToken.None);

        m_MockConsoleTable.Verify(t => t.AddRow(It.IsAny<Text[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task ListAsync_EmptyList_AddsPlaceholderRow()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>();

        var input = new CommonInput { CloudProjectId = k_ProjectId };

        await PurchasingListHandler.PurchasingListHandlerHandlerAsync(
            input,
            m_MockEnvironment.Object,
            m_FakeClient,
            m_MockConsoleTable.Object,
            m_MockLogger.Object,
            m_MockLoadingIndicator.Object,
            CancellationToken.None);

        m_MockConsoleTable.Verify(
            t => t.AddRow(
                It.Is<Text>(t1 => t1.ToString() == new Text("No items found").ToString()),
                It.IsAny<Text>(),
                It.IsAny<Text>(),
                It.IsAny<Text>(),
                It.IsAny<Text>(),
                It.IsAny<Text>()),
            Times.Once);
    }

    [Test]
    public async Task ListAsync_JsonMode_LogsResultInsteadOfTable()
    {
        m_FakeClient.RemoteItems = new List<CatalogItem>
        {
            new() { uSku = "sku_1", ProductType = ProductType.Consumable },
        };

        var input = new CommonInput { CloudProjectId = k_ProjectId, IsJson = true };

        await PurchasingListHandler.PurchasingListHandlerHandlerAsync(
            input,
            m_MockEnvironment.Object,
            m_FakeClient,
            m_MockConsoleTable.Object,
            m_MockLogger.Object,
            m_MockLoadingIndicator.Object,
            CancellationToken.None);

        m_MockLogger.Verify(
            l => l.Log(
                LogLevel.Critical,
                LoggerExtension.ResultEventId,
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        m_MockConsoleTable.Verify(t => t.AddColumns(It.IsAny<Text[]>()), Times.Never);
    }

    [Test]
    public async Task ListAsync_InitializesClientWithCorrectIds()
    {
        var input = new CommonInput { CloudProjectId = k_ProjectId };

        await PurchasingListHandler.PurchasingListHandlerHandlerAsync(
            input,
            m_MockEnvironment.Object,
            m_FakeClient,
            m_MockConsoleTable.Object,
            m_MockLogger.Object,
            m_MockLoadingIndicator.Object,
            CancellationToken.None);

        Assert.That(m_FakeClient.LastEnvironmentId, Is.EqualTo(k_EnvironmentId));
        Assert.That(m_FakeClient.LastProjectId, Is.EqualTo(k_ProjectId));
    }
}
