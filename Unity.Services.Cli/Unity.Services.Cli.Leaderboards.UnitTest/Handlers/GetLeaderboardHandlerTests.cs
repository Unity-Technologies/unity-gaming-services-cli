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
class GetLeaderboardHandlerTests
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

        m_MockLeaderboard.Setup(x => x.GetLeaderboardAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                CancellationToken.None))
            .ReturnsAsync(new ApiResponse<UpdatedLeaderboardConfig>(HttpStatusCode.OK,
                new UpdatedLeaderboardConfig(
                    id: k_LeaderboardId,
                    name: "foo",
                    sortOrder: SortOrder.Asc,
                    updateType: UpdateType.Aggregate,
                    bucketSize: Decimal.One,
                    resetConfig: new ResetConfig(),
                    tieringConfig: new TieringConfig(TieringConfig.StrategyEnum.Percent, new List<TieringConfigTiersInner>() { new TieringConfigTiersInner("tier1", 2) }),
                    updated: DateTime.Today,
                    created: DateTime.Now,
                    lastReset: DateTime.Today,
                    versions: new List<LeaderboardVersion>()
                    )));
    }

    [Test]
    public async Task GetHandlerAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetLeaderboardHandler.GetLeaderboardConfigAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetHandler_CallsGetServiceAndLogger()
    {
        LeaderboardIdInput input = new LeaderboardIdInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await GetLeaderboardHandler.GetLeaderboardConfigAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.GetLeaderboardAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, expectedTimes: Times.Once);
    }
}
