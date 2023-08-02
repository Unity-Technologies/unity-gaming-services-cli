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

namespace Unity.Services.Cli.Leaderboards.UnitTest.Handlers;

[TestFixture]
class DeleteLeaderboardHandlerTests
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
    }

    [Test]
    public async Task LoadDeleteAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeleteLeaderboardHandler.DeleteLeaderboardAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task DeleteHandler_CallsDeleteServiceSucceeded()
    {
        LeaderboardIdInput input = new LeaderboardIdInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = k_LeaderboardId
        };

        m_MockLeaderboard.Setup(x => x.DeleteLeaderboardAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            k_LeaderboardId,
            CancellationToken.None)).ReturnsAsync(new ApiResponse<object>(HttpStatusCode.NoContent, new object()));

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await DeleteLeaderboardHandler.DeleteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.DeleteLeaderboardAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                k_LeaderboardId,
                CancellationToken.None),
            Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
