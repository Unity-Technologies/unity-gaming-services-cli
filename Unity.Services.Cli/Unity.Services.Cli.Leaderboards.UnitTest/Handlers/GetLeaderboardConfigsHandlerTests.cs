using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Handlers;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.Leaderboards.UnitTest.Utils;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Handlers;

[TestFixture]
class GetLeaderboardConfigsHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboard = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboard.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task LoadListAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetLeaderboardConfigsHandler.GetLeaderboardConfigsAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task ListHandler_CallsListServiceAndLoggerWhenInputIsValid()
    {
        ListLeaderboardInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            Cursor = TestValues.Cursor,
            Limit = TestValues.Limit
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await GetLeaderboardConfigsHandler.GetLeaderboardConfigsAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            ex => ex.GetLeaderboardsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.Cursor, TestValues.Limit, CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }
}
