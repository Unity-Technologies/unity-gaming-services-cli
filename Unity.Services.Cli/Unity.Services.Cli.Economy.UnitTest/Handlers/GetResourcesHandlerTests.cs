using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Economy.Service;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Economy.Handlers;
using Unity.Services.Cli.Economy.UnitTest.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.UnitTest.Handlers;

public class GetResourcesHandlerTests
{
    Mock<IEconomyService>? m_MockEconomy;
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    Mock<ILogger>? m_MockLogger;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLogger = new Mock<ILogger>();
        m_MockEconomy = new Mock<IEconomyService>();
    }

    [Test]
    public async Task GetAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetResourcesHandler.GetAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetResourcesHandler_CallsServiceAndLogger_WhenInputIsValid()
    {
        CommonInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
        };


        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        CurrencyItemResponse currency = new CurrencyItemResponse(
            "id",
            "name",
            CurrencyItemResponse.TypeEnum.CURRENCY,
            0,
            100,
            "custom data",
            new ModifiedMetadata(DateTime.Now),
            new ModifiedMetadata(DateTime.Now)
        );

        GetResourcesResponseResultsInner response = new GetResourcesResponseResultsInner(currency);
        m_MockEconomy!.Setup(x => x.GetResourcesAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None))
            .ReturnsAsync(new List<GetResourcesResponseResultsInner> { response });

        await GetResourcesHandler.GetAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockEconomy!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockEconomy.Verify(ex => ex.GetResourcesAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

}
