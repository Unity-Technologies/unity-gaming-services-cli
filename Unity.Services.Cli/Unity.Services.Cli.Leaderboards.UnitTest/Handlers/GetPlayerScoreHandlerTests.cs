using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Handlers;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.Leaderboards.UnitTest.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboard.UnitTest.Handlers;

[TestFixture]
class GetPlayerScoreHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboard = new();
    readonly Mock<ILogger> m_MockLogger = new();
    const string k_LeaderboardId = "lb1";
    const string k_PlayerId = "player1";

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboard.Reset();
        m_MockLogger.Reset();

        m_MockLeaderboard.Setup(x => x.GetLeaderboardPlayerScoreAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                k_PlayerId,
                CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardEntryWithUpdatedTime>(HttpStatusCode.OK,
                new LeaderboardEntryWithUpdatedTime(DateTime.UtcNow, Guid.Empty, "player1", "Player 1", 100.0, 1, null)));
    }

    [Test]
    public async Task GetPlayerScoreAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetPlayerScoreHandler.GetPlayerScoreAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetPlayerScoreAsync_CallsServiceAndLogger()
    {
        PlayerScoreInput input = new PlayerScoreInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId,
            PlayerId = k_PlayerId
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await GetPlayerScoreHandler.GetPlayerScoreAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.GetLeaderboardPlayerScoreAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                k_PlayerId,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }

    [Test]
    public async Task GetPlayerScoreAsync_WithVersion_CallsVersionPlayerScoreService()
    {
        m_MockLeaderboard.Setup(x => x.GetLeaderboardVersionPlayerScoreAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "v1",
                k_PlayerId,
                CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionEntry>(HttpStatusCode.OK,
                new LeaderboardVersionEntry(
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    k_PlayerId, "Player 1", 100.0, 1, null)));

        PlayerScoreInput input = new PlayerScoreInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId,
            PlayerId = k_PlayerId,
            VersionId = "v1"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await GetPlayerScoreHandler.GetPlayerScoreAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.GetLeaderboardVersionPlayerScoreAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "v1",
                k_PlayerId,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }
}
