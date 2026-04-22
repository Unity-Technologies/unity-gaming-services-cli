using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Handlers;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Cli.Triggers.UnitTest.Utils;

namespace Unity.Services.Cli.Triggers.UnitTest.Handlers;

[TestFixture]
class DeleteTriggersHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ITriggersService> m_MockTriggersService = new();
    readonly Mock<ILogger> m_MockLogger = new();
    const string k_TriggerId = "00000000-0000-0000-0000-000000000001";

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockTriggersService.Reset();
        m_MockLogger.Reset();

        m_MockUnityEnvironment
            .Setup(x => x.FetchIdentifierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
    }

    [Test]
    public async Task DeleteAsync_CallsLoadingIndicator()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeleteTriggersHandler.DeleteAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CallsServiceAndLogsResult()
    {
        var input = new DeleteTriggerInput
        {
            CloudProjectId = TestValues.ValidProjectId,
            TriggerId = k_TriggerId,
        };

        m_MockTriggersService
            .Setup(s => s.DeleteTriggerAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_TriggerId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        await DeleteTriggersHandler.DeleteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockTriggersService.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(
            x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockTriggersService.Verify(
            s => s.DeleteTriggerAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_TriggerId,
                CancellationToken.None),
            Times.Once);
        m_MockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
