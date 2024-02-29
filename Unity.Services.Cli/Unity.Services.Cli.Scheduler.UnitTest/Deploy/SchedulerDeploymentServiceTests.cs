using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class SchedulerDeploymentServiceTests
{
    SchedulerDeploymentService? m_DeploymentService;
    readonly Mock<ISchedulerClient> m_MockScheduleClient = new();
    readonly Mock<IScheduleDeploymentHandler> m_MockScheduleDeploymentHandler = new();
    readonly Mock<IScheduleResourceLoader> m_MockScheduleConfigLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockScheduleClient.Reset();
        m_DeploymentService = new SchedulerDeploymentService(
            m_MockScheduleDeploymentHandler.Object,
            m_MockScheduleClient.Object,
            m_MockScheduleConfigLoader.Object);
    }

    [Test]
    public async Task DeployAsync_MapsResult()
    {
        var schedule1 = new ScheduleConfig("foo",
            "EventType1",
            "recurring",
            "0 * * * *",
            1,
            "{}");
        var schedule2 = new ScheduleConfig("bar",
            "EventType2",
            "recurring",
            "0 * * * *",
            1,
            "{}");

        var fileItem = new ScheduleFileItem(
            new ScheduleConfigFile(new Dictionary<string, ScheduleConfig>()
            {
                { "schedule1", schedule1 },
                { "schedule2", schedule2 }
            }),
            "path");
        m_MockScheduleConfigLoader
            .Setup(
                m =>
                    m.LoadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(fileItem);
        var deployResult = new DeployResult()
        {
            Created = new List<IScheduleConfig> { schedule2 },
            Updated = new List<IScheduleConfig>(),
            Deleted = new List<IScheduleConfig>(),
            Deployed = new List<IScheduleConfig> { schedule2 },
            Failed = new List<IScheduleConfig>()
        };
        m_MockScheduleDeploymentHandler.Setup(
                d => d.DeployAsync(
                    It.IsAny<IReadOnlyList<IScheduleConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult(deployResult));

        var input = new DeployInput()
        {
            CloudProjectId = string.Empty
        };
        var res = await m_DeploymentService!.Deploy(
            input,
            new[] { "path"},
            String.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(res.Created.Count, Is.EqualTo(1));
            Assert.That(res.Updated.Count, Is.EqualTo(0));
            Assert.That(res.Deleted.Count, Is.EqualTo(0));
            Assert.That(res.Deployed.Count, Is.EqualTo(1));
            Assert.That(res.Failed.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task DeployAsync_MapsFailed()
    {
        m_MockScheduleConfigLoader
            .Setup(
                m =>
                    m.LoadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ScheduleFileItem(new ScheduleConfigFile(
                    new Dictionary<string, ScheduleConfig>()),
                "scheduleFile.sched",
                status: new DeploymentStatus("failed", "failed", SeverityLevel.Error)));
        var deployResult = new DeployResult()
        {
            Created = new List<IScheduleConfig>(),
            Updated = new List<IScheduleConfig>(),
            Deleted = new List<IScheduleConfig>(),
            Deployed = new List<IScheduleConfig>(),
            Failed = new List<IScheduleConfig>()
        };
        m_MockScheduleDeploymentHandler.Setup(
                d => d.DeployAsync(
                    It.IsAny<IReadOnlyList<IScheduleConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult(deployResult));

        var input = new DeployInput()
        {
            CloudProjectId = string.Empty
        };
        var res = await m_DeploymentService!.Deploy(
            input,
            new[] { "dir" },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(res.Created.Count, Is.EqualTo(0));
            Assert.That(res.Updated.Count, Is.EqualTo(0));
            Assert.That(res.Deleted.Count, Is.EqualTo(0));
            Assert.That(res.Deployed.Count, Is.EqualTo(0));
            Assert.That(res.Failed.Count, Is.EqualTo(1));
        });
    }
}
