using NUnit.Framework;
using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Fetch;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
public class LeaderboardFetchServiceTests
{
    LeaderboardFetchService? m_FetchService;
    readonly Mock<ILeaderboardsClient> m_MockLeaderboardClient = new();
    readonly Mock<ILeaderboardsFetchHandler> m_MockLeaderboardFetchHandler = new();
    readonly Mock<ILeaderboardsConfigLoader> m_MockLeaderboardConfigLoader = new();
    readonly Mock<IDeployFileService> m_MockDeployFileService = new();
    readonly Mock<IUnityEnvironment> m_UnityEnvironment = new();

    [SetUp]
    public void SetUp()
    {
        m_MockLeaderboardClient.Reset();
        m_FetchService = new LeaderboardFetchService(
            m_MockLeaderboardClient.Object,
            m_MockLeaderboardFetchHandler.Object,
            m_MockLeaderboardConfigLoader.Object,
            m_MockDeployFileService.Object,
            m_UnityEnvironment.Object);

        var lb1 = new LeaderboardConfig("lb1", "LB1");
        var lb2 = new LeaderboardConfig("lb2", "LB2");

        var mockLoad = (IReadOnlyList<LeaderboardConfig>) new[] { lb1 };
        var failedToLoad = (IReadOnlyList<LeaderboardConfig>)Array.Empty<LeaderboardConfig>();

        m_MockLeaderboardConfigLoader
            .Setup(
                m =>
                    m.LoadConfigsAsync(
                        It.IsAny<IReadOnlyList<string>>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((mockLoad, failedToLoad));

        var deployResult = new FetchResult()
        {
            Created = new List<ILeaderboardConfig> { lb2 },
            Updated = new List<ILeaderboardConfig>(),
            Deleted = new List<ILeaderboardConfig>(),
            Fetched = new List<ILeaderboardConfig> { lb2 },
            Failed = new List<ILeaderboardConfig>()
        };
        var fromResult = Task.FromResult(deployResult);

        m_MockLeaderboardFetchHandler.Setup(
                d => d.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<ILeaderboardConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(fromResult);
    }

    [Test]
    public async Task FetchAsync_MapsResult()
    {
        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
            input,
            new[] { "dir" },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.AreEqual(1, res.Created.Count);
        Assert.AreEqual(0, res.Updated.Count);
        Assert.AreEqual(0, res.Deleted.Count);
        Assert.AreEqual(1, res.Fetched.Count);
        Assert.AreEqual(0, res.Failed.Count);
    }

    [Test]
    public async Task FetchAsync_MapsFailed()
    {
        var lb1 = new LeaderboardConfig("lb1", "LB1");
        var lbFailed = new LeaderboardConfig("failed_lb", "Failed");
        var mockLoad = (IReadOnlyList<LeaderboardConfig>) new[] { lb1 };
        var failedToLoad = (IReadOnlyList<LeaderboardConfig>) new[] { lbFailed };

        m_MockLeaderboardConfigLoader
            .Setup(
                m =>
                    m.LoadConfigsAsync(
                        It.IsAny<IReadOnlyList<string>>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((mockLoad, failedToLoad));

        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
            input,
            new[] { "dir" },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.AreEqual(1, res.Created.Count);
        Assert.AreEqual(0, res.Updated.Count);
        Assert.AreEqual(0, res.Deleted.Count);
        Assert.AreEqual(1, res.Fetched.Count);
        Assert.AreEqual(1, res.Failed.Count);
    }
}
