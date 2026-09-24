using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;
using CoreDeploymentResult = Unity.Services.DeploymentApi.Editor.DeploymentResult<Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model.SchedulerEntryDeploymentItem>;
using Statuses = Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model.Statuses;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class SchedulerDeploymentServiceTests
{
    SchedulerDeploymentService? m_DeploymentService;
    readonly Mock<ISchedulerClient> m_MockScheduleClient = new();
    readonly Mock<ISchedulerDeploymentHandler> m_MockScheduleDeploymentHandler = new();
    readonly Mock<ISchedulerResourceLoader> m_MockScheduleConfigLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockScheduleClient.Reset();
        m_MockScheduleDeploymentHandler.Reset();
        m_MockScheduleConfigLoader.Reset();
        m_DeploymentService = new SchedulerDeploymentService(
            m_MockScheduleDeploymentHandler.Object,
            m_MockScheduleClient.Object,
            m_MockScheduleConfigLoader.Object);
    }

    [Test]
    public async Task DeployAsync_MapsResult()
    {
        var schedule1 = new SchedulerEntry
        {
            Id = "foo",
            Name = "schedule1",
            EventName = "EventType1",
            ScheduleType = "recurring",
            Schedule = "0 * * * *",
            PayloadVersion = 1,
            Payload = "{}"
        };
        var schedule2 = new SchedulerEntry
        {
            Id = "bar",
            Name = "schedule2",
            EventName = "EventType2",
            ScheduleType = "recurring",
            Schedule = "0 * * * *",
            PayloadVersion = 1,
            Payload = "{}"
        };

        var files = new List<SchedulerEntryDeploymentItem>
        {
            new("schedule1.sched")
            {
                entry = schedule1
            },
            new("schedule2.sched")
            {
                entry = schedule2,
                Status = Statuses.GetDeployed(Constants.Created)
            }
        };

        m_MockScheduleConfigLoader
            .Setup(m =>
                m.ReadResource(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(files);

        var deployResult = new CoreDeploymentResult([files[1]]);
        m_MockScheduleDeploymentHandler.Setup(d => d.DeployAsync(
                It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
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
            [new AuthoringFile("path")],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(res.Created, Has.Count.EqualTo(1));
            Assert.That(res.Updated, Is.Empty);
            Assert.That(res.Deleted, Is.Empty);
            Assert.That(res.Deployed, Has.Count.EqualTo(1));
            Assert.That(res.Failed, Is.Empty);
        });
    }

    [Test]
    public async Task DeployAsync_MapsFailed()
    {
        var failedItem = new SchedulerEntryDeploymentItem("scheduleFile.sched")
        {
            Status = new DeploymentStatus("failed", "failed", SeverityLevel.Error)
        };
        m_MockScheduleConfigLoader
            .Setup(m =>
                m.ReadResource(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([failedItem]);
        var deployResult = new CoreDeploymentResult();
        m_MockScheduleDeploymentHandler.Setup(d => d.DeployAsync(
                It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
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
            [new AuthoringFile("dir")],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(res.Created, Is.Empty);
            Assert.That(res.Updated, Is.Empty);
            Assert.That(res.Deleted, Is.Empty);
            Assert.That(res.Deployed, Is.Empty);
            Assert.That(res.Failed, Has.Count.EqualTo(2));
        });
    }
}
