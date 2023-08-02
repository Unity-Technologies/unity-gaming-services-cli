using NUnit.Framework;
using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
public class LeaderboardDeploymentServiceTests
{
    LeaderboardDeploymentService? m_DeploymentService;
    readonly Mock<ILeaderboardsClient> m_MockLeaderboardClient = new();
    readonly Mock<ILeaderboardsDeploymentHandler> m_MockLeaderboardDeploymentHandler = new();
    readonly Mock<ILeaderboardsConfigLoader> m_MockLeaderboardConfigLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockLeaderboardClient.Reset();
        m_DeploymentService = new LeaderboardDeploymentService(
            m_MockLeaderboardClient.Object,
            m_MockLeaderboardDeploymentHandler.Object,
            m_MockLeaderboardConfigLoader.Object);

        var lb1 = new LeaderboardConfig("lb1", "LB1");
        var lb2 = new LeaderboardConfig("lb2", "LB2");

        var mockLoad = Task.FromResult(
            (IReadOnlyList<LeaderboardConfig>)(new[]
            {
                lb1
            }));

        m_MockLeaderboardConfigLoader
            .Setup(
                m =>
                    m.LoadConfigsAsync(
                        It.IsAny<IReadOnlyList<string>>(),
                        It.IsAny<CancellationToken>())
            )
            .Returns(mockLoad);

        var deployResult = new DeployResult()
        {
            Created = new List<ILeaderboardConfig> { lb2 },
            Updated = new List<ILeaderboardConfig>(),
            Deleted = new List<ILeaderboardConfig>(),
            Deployed = new List<ILeaderboardConfig> { lb2 },
            Failed = new List<ILeaderboardConfig>()
        };
        var fromResult = Task.FromResult(deployResult);

        m_MockLeaderboardDeploymentHandler.Setup(
                d => d.DeployAsync(
                    It.IsAny<IReadOnlyList<ILeaderboardConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(fromResult);
    }

    [Test]
    public async Task DeployAsync_MapsResult()
    {
        var input = new DeployInput()
        {
            Paths = Array.Empty<string>(),
            CloudProjectId = string.Empty
        };
        var res = await m_DeploymentService!.Deploy(
            input,
            Array.Empty<string>(),
            String.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.AreEqual(1, res.Created.Count);
        Assert.AreEqual(0, res.Updated.Count);
        Assert.AreEqual(0, res.Deleted.Count);
        Assert.AreEqual(1, res.Deployed.Count);
        Assert.AreEqual(0, res.Failed.Count);
    }
}
