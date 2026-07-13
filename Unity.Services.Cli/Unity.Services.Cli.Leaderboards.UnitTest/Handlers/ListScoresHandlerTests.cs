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
class ListScoresHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboard = new();
    readonly Mock<ILogger> m_MockLogger = new();
    const string k_LeaderboardId = "lb1";

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboard.Reset();
        m_MockLogger.Reset();

        m_MockLeaderboard.Setup(x => x.GetLeaderboardScoresAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardScoresPage>(HttpStatusCode.OK,
                new LeaderboardScoresPage(0, 10, 0, new List<LeaderboardEntry>())));
    }

    [Test]
    public async Task ListScoresAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await ListScoresHandler.ListScoresAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task ListScoresAsync_CallsServiceAndLogger()
    {
        PaginatedLeaderboardInput input = new PaginatedLeaderboardInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ListScoresHandler.ListScoresAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.GetLeaderboardScoresAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }

    [Test]
    public async Task ListScoresAsync_WithTier_CallsScoresByTierService()
    {
        m_MockLeaderboard.Setup(x => x.GetLeaderboardScoresByTierAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "tier1",
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardTierScoresPage>(HttpStatusCode.OK,
                new LeaderboardTierScoresPage("tier1", 0, 10, 0, new List<LeaderboardEntry>())));

        PaginatedLeaderboardInput input = new PaginatedLeaderboardInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId,
            TierId = "tier1"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ListScoresHandler.ListScoresAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.GetLeaderboardScoresByTierAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "tier1",
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }

    [Test]
    public async Task ListScoresAsync_WithVersion_CallsVersionScoresService()
    {
        m_MockLeaderboard.Setup(x => x.GetLeaderboardVersionScoresAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "v1",
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionScoresPage>(HttpStatusCode.OK,
                new LeaderboardVersionScoresPage(
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    0, 10, 0, new List<LeaderboardEntry>())));

        PaginatedLeaderboardInput input = new PaginatedLeaderboardInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId,
            VersionId = "v1"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ListScoresHandler.ListScoresAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.GetLeaderboardVersionScoresAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "v1",
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }

    [Test]
    public async Task ListScoresAsync_WithVersionAndTier_CallsVersionScoresByTierService()
    {
        m_MockLeaderboard.Setup(x => x.GetLeaderboardVersionScoresByTierAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "v1",
                "tier1",
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None))
            .ReturnsAsync(new ApiResponse<LeaderboardVersionTierScoresPage>(HttpStatusCode.OK,
                new LeaderboardVersionTierScoresPage(
                    "tier1",
                    new LeaderboardVersion("v1", DateTime.UtcNow, DateTime.UtcNow),
                    0, 10, 0, new List<LeaderboardEntry>())));

        PaginatedLeaderboardInput input = new PaginatedLeaderboardInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId,
            VersionId = "v1",
            TierId = "tier1"
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ListScoresHandler.ListScoresAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.GetLeaderboardVersionScoresByTierAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                "v1",
                "tier1",
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }
}
