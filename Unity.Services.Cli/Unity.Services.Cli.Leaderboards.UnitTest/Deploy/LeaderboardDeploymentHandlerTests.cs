using Moq;
using NUnit.Framework;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
class LeaderboardDeploymentHandlerTests
{
    [Test]
    public async Task DeployAsync_CorrectResult()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards
        );

        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "foo"), actualRes.Updated);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "foo"), actualRes.Deployed);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "bar"), actualRes.Created);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "bar"), actualRes.Deployed);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "dup-id"), actualRes.Created);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "dup-id"), actualRes.Deployed);
    }

    [Test]
    public async Task DeployAsync_CreateCallsMade()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards
        );

        mockLeaderboardsClient
            .Verify(
                c => c.Create(
                    It.Is<ILeaderboardConfig>(l => l.Id == "bar"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        mockLeaderboardsClient
            .Verify(
                c => c.Create(
                    It.Is<ILeaderboardConfig>(l => l.Id == "dup-id"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_UpdateCallsMade()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards
        );

        mockLeaderboardsClient
            .Verify(
                c => c.Update(
                    It.Is<ILeaderboardConfig>(l => l.Id == "foo"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_NoReconcileNoDeleteCalls()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards
        );

        mockLeaderboardsClient
            .Verify(
                c => c.Delete(
                    It.Is<ILeaderboardConfig>(l => l.Id == "echo"),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_ReconcileDeleteCalls()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards,
            reconcile: true
        );

        mockLeaderboardsClient
            .Verify(
                c => c.Delete(
                    It.Is<ILeaderboardConfig>(l => l.Id == "echo"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
    }


    [Test]
    public async Task DeployAsync_DryRunNoCalls()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards,
            true
        );

        mockLeaderboardsClient
            .Verify(
                c => c.Create(
                    It.IsAny<ILeaderboardConfig>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        mockLeaderboardsClient
            .Verify(
                c => c.Update(
                    It.IsAny<ILeaderboardConfig>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

        mockLeaderboardsClient
            .Verify(
                c => c.Delete(
                    It.IsAny<ILeaderboardConfig>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_DryRunCorrectResult()
    {
        var localLeaderboards = GetLocalConfigs();
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards,
            dryRun: true
        );

        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "foo"), actualRes.Updated);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "bar"), actualRes.Created);
        Assert.Contains(localLeaderboards.FirstOrDefault(l => l.Id == "dup-id"), actualRes.Created);
        Assert.AreEqual(0, actualRes.Deployed.Count);
    }

    [Test]
    public async Task FetchAsync_DuplicateIdNotDeleted()
    {
        var localLeaderboards = GetLocalConfigs();
        localLeaderboards.Add(new LeaderboardConfig("dup-id", "other name") { Path = "otherpath.lb"});
        var remoteLeaderboards = GetRemoteConfigs();

        Mock<ILeaderboardsClient> mockLeaderboardsClient = new();
        var handler = new LeaderboardsDeploymentHandler(mockLeaderboardsClient.Object);

        mockLeaderboardsClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteLeaderboards.ToList());

        var actualRes = await handler.DeployAsync(
            localLeaderboards,
            dryRun: true
        );

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

    static IReadOnlyList<ILeaderboardConfig> GetRemoteConfigs()
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
