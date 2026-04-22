using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Triggers.Handlers;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Cli.Triggers.UnitTest.Utils;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.UnitTest.Handlers;

[TestFixture]
class ListTriggersHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ITriggersService> m_MockTriggersService = new();
    readonly Mock<ILogger> m_MockLogger = new();

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
    public async Task ListAsync_CallsLoadingIndicator()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await ListTriggersHandler.ListAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task ListAsync_CallsServiceAndLogsResult()
    {
        var input = new ListTriggersInput
        {
            CloudProjectId = TestValues.ValidProjectId,
            Limit = TestValues.Limit,
        };

        var triggers = new List<TriggerConfigListItem>
        {
            new(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "Trigger1",
                "eventType", "cloud-code", "urn:ugs:cloud-code:Script1"),
        };

        m_MockTriggersService
            .Setup(s => s.GetTriggersAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.Limit,
                CancellationToken.None))
            .ReturnsAsync(triggers);

        await ListTriggersHandler.ListAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockTriggersService.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(
            x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockTriggersService.Verify(
            s => s.GetTriggersAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.Limit,
                CancellationToken.None),
            Times.Once);
        m_MockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                LoggerExtension.ResultEventId,
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
