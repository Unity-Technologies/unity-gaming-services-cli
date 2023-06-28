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
class UpdateLeaderboardHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboard = new();
    readonly Mock<ILogger> m_MockLogger = new();
    const string leaderboardId = "lb1";
    string leaderboardPath = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboard.Reset();
        m_MockLogger.Reset();
        leaderboardPath = Directory.GetCurrentDirectory() + "/leaderboardBodyUpdate.txt";
    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(leaderboardPath);
    }

    [Test]
    public async Task LoadUpdateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await UpdateLeaderboardHandler.UpdateLeaderboardAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task UpdateHandler_CallsUpdateServiceSucceeded()
    {
        await File.WriteAllTextAsync(leaderboardPath, "{}", new UTF8Encoding(true));
        UpdateInput input = new UpdateInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            LeaderboardId = leaderboardId,
            JsonFilePath = leaderboardPath
        };

        m_MockLeaderboard.Setup(x => x.UpdateLeaderboardAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            leaderboardId,
            "{}",
            CancellationToken.None)).ReturnsAsync(new ApiResponse<object>(HttpStatusCode.NoContent, new object()));

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await UpdateLeaderboardHandler.UpdateAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.UpdateLeaderboardAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                leaderboardId,
                "{}",
                CancellationToken.None),
            Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
