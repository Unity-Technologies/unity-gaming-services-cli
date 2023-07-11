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
class CreateLeaderboardHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ILeaderboardsService> m_MockLeaderboard = new();
    readonly Mock<ILogger> m_MockLogger = new();
    string leaderboardPath = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockLeaderboard.Reset();
        m_MockLogger.Reset();
        leaderboardPath = Directory.GetCurrentDirectory() + "/leaderboardBody.txt";
    }

    [TearDown]
    public void TearDown()
    {
        File.Delete(leaderboardPath);
    }


    [Test]
    public async Task LoadCreateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await CreateLeaderboardHandler.CreateLeaderboardAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task CreateHandler_CallsCreateServiceSucceeded()
    {
        await File.WriteAllTextAsync(leaderboardPath, "{}", new UTF8Encoding(true));

        CreateInput input = new CreateInput()
        {
            CloudProjectId = TestValues.ValidProjectId,
            JsonFilePath = leaderboardPath
        };

        m_MockLeaderboard.Setup(x => x.CreateLeaderboardAsync(
            TestValues.ValidProjectId,
            TestValues.ValidEnvironmentId,
            "{}",
            CancellationToken.None)).ReturnsAsync(new ApiResponse<object>(HttpStatusCode.Created, new object()));

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await CreateLeaderboardHandler.CreateAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockLeaderboard.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockLeaderboard.Verify(
            e => e.CreateLeaderboardAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                "{}",
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
