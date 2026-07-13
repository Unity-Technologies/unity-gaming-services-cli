using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Observability.Handlers;
using Unity.Services.Cli.Observability.Input;
using Unity.Services.Cli.Observability.Service;
using Unity.Services.Cli.Observability.UnitTest.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Client;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;

namespace Unity.Services.Cli.Observability.UnitTest.Handlers;

[TestFixture]
class GetLogsHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IObservabilityService> m_MockObservability = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockObservability.Reset();
        m_MockLogger.Reset();

        m_MockObservability.Setup(x => x.GetLogsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<LogsResponse>(
                HttpStatusCode.OK,
                new LogsResponse(0, 100, 1, new List<LogRecord>
                {
                    new("2023-09-06T10:02:24.904Z", "Error", 17, "Hello, world!", null, null)
                })));
    }

    [Test]
    public async Task GetLogsHandlerAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetLogsHandler.GetLogsAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetLogsHandler_CallsServiceAndLogger()
    {
        var input = new ListLogsInput
        {
            CloudProjectId = TestValues.ValidProjectId,
            From = "now-3h",
            Limit = 50,
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await GetLogsHandler.GetLogsAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockObservability.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockObservability.Verify(
            e => e.GetLogsAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                "now-3h",
                null,
                null,
                null,
                50,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }
}
