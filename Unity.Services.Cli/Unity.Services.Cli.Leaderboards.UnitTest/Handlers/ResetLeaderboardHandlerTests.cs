using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Handlers;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Cli.Leaderboards.UnitTest.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Client;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Handlers;

[TestFixture]
class ResetLeaderboardHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboard = new();
    readonly Mock<ILogger> m_MockLogger = new();
    const string leaderboardId = "lb1";

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboard.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task LoadResetAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await ResetLeaderboardHandler.ResetLeaderboardAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task ResetHandler_CallsResetServiceAndLogger()
    {
        ResetInput input = new ResetInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = leaderboardId
        };

        m_MockLeaderboard.Setup(x => x.ResetLeaderboardAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            leaderboardId,
            null,
            CancellationToken.None)).ReturnsAsync(new ApiResponse<LeaderboardVersionId>(HttpStatusCode.OK, new LeaderboardVersionId(), "{ \"versionId\": 10 }"));

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await ResetLeaderboardHandler.ResetAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.ResetLeaderboardAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                leaderboardId,
                null,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
