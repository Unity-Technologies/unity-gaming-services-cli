using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Fetch;
using Unity.Services.Leaderboards.Authoring.Core.IO;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
class LeaderboardFetchHandlerTests
{
    [Test]
    public async Task FetchAsync_CorrectResult()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new LeaderboardsFetchHandler(mockLeaderboardsClient.Object, mockFileSystem.Object, new LeaderboardsSerializer());

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localLeaderboards
        );

        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "foo"), actualRes.Updated);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "foo"), actualRes.Fetched);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "bar"), actualRes.Deleted);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "bar"), actualRes.Fetched);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "dup-id"), actualRes.Deleted);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "dup-id"), actualRes.Fetched);
        Assert.IsEmpty(actualRes.Created);
    }

    [Test]
    public async Task FetchAsync_WriteCallsMade()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new LeaderboardsFetchHandler(mockLeaderboardsClient.Object, mockFileSystem.Object, new LeaderboardsSerializer());

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localLeaderboards
        );

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    "path1",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    "echo",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);  //Should not happen unless reconcile
    }

    [Test]
    public async Task FetchAsync_DeleteCallsMade()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new LeaderboardsFetchHandler(mockLeaderboardsClient.Object, mockFileSystem.Object, new LeaderboardsSerializer());

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localLeaderboards
        );

        mockFileSystem
            .Verify(f => f.Delete(
                    "path2",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        mockFileSystem
            .Verify(f => f.Delete(
                    "path3",
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Test]
    public async Task FetchAsync_WriteNewOnReconcile()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new LeaderboardsFetchHandler(mockLeaderboardsClient.Object, mockFileSystem.Object, new LeaderboardsSerializer());

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localLeaderboards,
            reconcile: true
        );

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    "path1",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    Path.Combine("dir","echo.lb"),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);  //Should happen on reconcile
    }

    [Test]
    public async Task FetchAsync_DryRunNoCalls()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new LeaderboardsFetchHandler(mockLeaderboardsClient.Object, mockFileSystem.Object, new LeaderboardsSerializer());

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localLeaderboards,
            dryRun: true
        );

        mockFileSystem
            .Verify(f => f.Delete(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    public async Task FetchAsync_DuplicateIdNotDeleted()
    {
        var localLeaderboards = GetLocalConfigs();
        localLeaderboards.Add(new LeaderboardConfig("dup-id", "other name") { Path = "otherpath.lb"});
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new LeaderboardsFetchHandler(mockLeaderboardsClient.Object, mockFileSystem.Object, new LeaderboardsSerializer());

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localLeaderboards,
            dryRun: true
        );

        mockFileSystem
            .Verify(f => f.Delete(
                    It.Is<string>(s => s == "path3"),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Name == "other name"), actualRes.Failed);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Name == "dup-id"), actualRes.Failed);
    }

    static List<ILeaderboardConfig> GetLocalConfigs()
    {
        var leaderboards = new List<ILeaderboardConfig>()
        {
            new LeaderboardConfig("foo", "foo")
            {
                Path = "path1"
            },
            new LeaderboardConfig("bar", "bar")
            {
                Path = "path2"
            },
            new LeaderboardConfig("dup-id", "dup-id")
            {
                Path = "path3"
            }
        };
        return leaderboards;
    }

    static List<ILeaderboardConfig> GetRemoteConfigs()
    {
        var leaderboards = new List<ILeaderboardConfig>()
        {
            new LeaderboardConfig("foo", "foo")
            {
                Path = "Remote"
            },
            new LeaderboardConfig("echo", "echo")
            {
                Path = "Remote"
            }
        };
        return leaderboards;
    }
}
