using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Economy.Service;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Economy.Handlers;
using Unity.Services.Cli.Economy.UnitTest.Utils;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Economy.UnitTest.Handlers;

public class PublishHandlerTests
{
    readonly Mock<IEconomyService>? m_MockEconomy = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILogger>? m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLogger.Reset();
        m_MockEconomy.Reset();
    }

    [Test]
    public async Task PublishAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await PublishHandler.PublishAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task PublishHandler_CallsServiceAndLogger_WhenInputIsValid()
    {
        CommonInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
        };


        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockEconomy!.Setup(x =>
            x.PublishAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));

        await PublishHandler.PublishAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockEconomy!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockEconomy.Verify(ex => ex.PublishAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            CancellationToken.None), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }

}
