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
class GetTriggerHandlerTests
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
    public async Task GetAsync_CallsLoadingIndicator()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetTriggerHandler.GetAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task GetAsync_CallsServiceAndLogsResult()
    {
        var input = new GetTriggerInput
        {
            CloudProjectId = TestValues.ValidProjectId,
            TriggerId = k_TriggerId,
        };

        var config = new TriggerConfig(
            Guid.Parse(k_TriggerId),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Test Trigger",
            Guid.Parse(TestValues.ValidProjectId),
            Guid.Parse(TestValues.ValidEnvironmentId),
            "com.unity.services.scheduler.example-event.v1",
            "cloud-code",
            "urn:ugs:cloud-code:MyTestScript");

        m_MockTriggersService
            .Setup(s => s.GetTriggerAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_TriggerId,
                CancellationToken.None))
            .ReturnsAsync(config);

        await GetTriggerHandler.GetAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockTriggersService.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(
            x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockTriggersService.Verify(
            s => s.GetTriggerAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_TriggerId,
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
