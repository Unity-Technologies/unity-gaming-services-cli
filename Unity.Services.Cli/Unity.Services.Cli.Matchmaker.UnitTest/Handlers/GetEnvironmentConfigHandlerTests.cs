using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Matchmaker.Handlers;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Cli.TestUtils;
using Generated = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model;

namespace Unity.Services.Cli.Matchmaker.UnitTest.Handlers;

[TestFixture]
class GetEnvironmentConfigHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<IMatchmakerService> m_MockService = new();
    readonly Mock<ILogger> m_MockLogger = new();

    const string k_ProjectId = "00000000-0000-0000-0000-000000000000";
    const string k_EnvironmentId = "00000000-0000-0000-0000-000000000000";

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockService.Reset();
        m_MockLogger.Reset();

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(k_EnvironmentId);
        m_MockService.Setup(x => x.Initialize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        m_MockService.Setup(x => x.GetEnvironmentConfig(It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, new Generated.EnvironmentConfig { Enabled = true, DefaultQueueName = "default" }));
    }

    [Test]
    public async Task GetEnvironmentConfigAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetEnvironmentConfigHandler.GetEnvironmentConfigAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetEnvironmentConfigAsync_CallsServiceAndLogger()
    {
        var input = new CommonInput { CloudProjectId = k_ProjectId };

        await GetEnvironmentConfigHandler.GetEnvironmentConfigAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockService.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockService.Verify(x => x.Initialize(k_ProjectId, k_EnvironmentId, CancellationToken.None), Times.Once);
        m_MockService.Verify(x => x.GetEnvironmentConfig(CancellationToken.None), Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }
}
