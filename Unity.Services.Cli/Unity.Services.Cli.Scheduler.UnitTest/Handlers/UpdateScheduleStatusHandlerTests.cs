using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Scheduler.Handlers;
using Unity.Services.Cli.Scheduler.Input;
using Unity.Services.Cli.Scheduler.Service;
using Unity.Services.Cli.Scheduler.UnitTest.Utils;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;

namespace Unity.Services.Cli.Scheduler.UnitTest.Handlers;

[TestFixture]
class UpdateScheduleStatusHandlerTests
{
    const string k_ScheduleId = "00000000-0000-0000-0000-000000000001";
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ISchedulerStatusClient> m_MockClient = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockClient.Reset();
        m_MockLogger.Reset();

        m_MockUnityEnvironment
            .Setup(x => x.FetchIdentifierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
    }

    [Test]
    public async Task ActivateAsync_CallsLoadingIndicator()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await UpdateScheduleStatusHandler.ActivateAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task DeactivateAsync_CallsLoadingIndicator()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await UpdateScheduleStatusHandler.DeactivateAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [TestCase(ScheduleStatusUpdate.Active)]
    [TestCase(ScheduleStatusUpdate.Inactive)]
    public async Task UpdateStatusAsync_CallsClientAndLogsResult(ScheduleStatusUpdate status)
    {
        var input = new UpdateScheduleStatusInput
        {
            CloudProjectId = TestValues.ValidProjectId,
            ScheduleId = k_ScheduleId,
        };

        await UpdateScheduleStatusHandler.UpdateStatusAsync(
            input,
            status,
            m_MockUnityEnvironment.Object,
            m_MockClient.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockClient.Verify(
            c => c.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None),
            Times.Once);
        m_MockClient.Verify(c => c.UpdateStatus(k_ScheduleId, status, CancellationToken.None), Times.Once);
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
