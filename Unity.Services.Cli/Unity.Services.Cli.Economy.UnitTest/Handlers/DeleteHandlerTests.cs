using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Economy.Service;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Economy.Handlers;
using Unity.Services.Cli.Economy.Input;
using Unity.Services.Cli.Economy.UnitTest.Utils;

namespace Unity.Services.Cli.Economy.UnitTest.Handlers;

public class DeleteHandlerTests
{
    readonly Mock<IEconomyService>? m_MockEconomy = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILogger>? m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockEconomy.Reset();
        m_MockUnityEnvironment.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task DeleteAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeleteHandler.DeleteAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task DeleteHandler_CallsServiceAndLogger_WhenInputIsValid()
    {
        var resourceId = "resource_id";

        EconomyInput input = new()
        {
            ResourceId = resourceId,
            CloudProjectId = TestValues.ValidProjectId,
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        m_MockEconomy!.Setup(x => x.DeleteAsync(resourceId, TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            CancellationToken.None));

        await DeleteHandler.DeleteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockEconomy!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockEconomy.Verify(ex => ex.DeleteAsync(resourceId, TestValues.ValidProjectId, TestValues.ValidEnvironmentId,
            CancellationToken.None), Times.Once);
    }

}
