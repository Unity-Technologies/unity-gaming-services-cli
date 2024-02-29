using Moq;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
class SchedulerDeploymentHandlerTests
{
    [Test]
    public async Task DeployAsync_CorrectResult()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.DeployAsync(
            localSchedules
        );

        Assert.Contains(localSchedules.FirstOrDefault(l => l.Id == "foo"), actualRes.Updated);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Id == "foo"), actualRes.Deployed);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Id == "bar"), actualRes.Created);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Id == "bar"), actualRes.Deployed);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Id == "dup-id"), actualRes.Created);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Id == "dup-id"), actualRes.Deployed);
    }

    [Test]
    public async Task DeployAsync_CreateCallsMade()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        await handler.DeployAsync(
            localSchedules
        );

        mockSchedulesClient
            .Verify(
                c => c.Create(
                    It.Is<IScheduleConfig>(l => l.Id == "bar")),
                Times.Once);
        mockSchedulesClient
            .Verify(
                c => c.Create(
                    It.Is<IScheduleConfig>(l => l.Id == "dup-id")),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_UpdateCallsMade()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        await handler.DeployAsync(
            localSchedules
        );

        mockSchedulesClient
            .Verify(
                c => c.Update(
                    It.Is<IScheduleConfig>(l => l.Id == "foo")),
                Times.Once);
    }

    [Test]
    public async Task DeployAsync_StatusesSet()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.DeployAsync(
            localSchedules,
            reconcile: true
        );

        mockSchedulesClient
            .Verify(
                c => c.Update(
                    It.Is<IScheduleConfig>(l => l.Id == "foo")),
                Times.Once);
        var expectedCreatedSchedule = actualRes.Deployed.FirstOrDefault(l => l.Id == "bar");
        Assert.IsTrue(expectedCreatedSchedule.Status.Message == "Deployed");
        Assert.IsTrue(expectedCreatedSchedule.Status.MessageDetail == "Created");

        var expectedUpdatedSchedule = actualRes.Deployed.FirstOrDefault(l => l.Id == "foo");
        Assert.IsTrue(expectedUpdatedSchedule.Status.Message == "Deployed");
        Assert.IsTrue(expectedUpdatedSchedule.Status.MessageDetail == "Updated");

        var expectedDeletedSchedule = actualRes.Deployed.FirstOrDefault(l => l.Id == "echo");
        Assert.IsTrue(expectedDeletedSchedule.Status.Message == "Deployed");
        Assert.IsTrue(expectedDeletedSchedule.Status.MessageDetail == "Deleted");
    }

    [Test]
    public async Task DeployAsync_NoReconcileNoDeleteCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        await handler.DeployAsync(
            localSchedules
        );

        mockSchedulesClient
            .Verify(
                c => c.Delete(
                    It.Is<IScheduleConfig>(l => l.Id == "echo")),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_ReconcileDeleteCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        await handler.DeployAsync(
            localSchedules,
            reconcile: true
        );

        mockSchedulesClient
            .Verify(
                c => c.Delete(
                    It.Is<IScheduleConfig>(l => l.Id == "echo")),
                Times.Once);
    }


    [Test]
    public async Task DeployAsync_DryRunNoCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        await handler.DeployAsync(
            localSchedules,
            true
        );

        mockSchedulesClient
            .Verify(
                c => c.Create(
                    It.IsAny<IScheduleConfig>()),
                Times.Never);

        mockSchedulesClient
            .Verify(
                c => c.Update(
                    It.IsAny<IScheduleConfig>()),
                Times.Never);

        mockSchedulesClient
            .Verify(
                c => c.Delete(
                    It.IsAny<IScheduleConfig>()),
                Times.Never);
    }

    [Test]
    public async Task DeployAsync_DryRunCorrectResult()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.DeployAsync(
            localSchedules,
            dryRun: true
        );
        Assert.Multiple(() =>
        {
            Assert.That(actualRes.Updated, Does.Contain(localSchedules.FirstOrDefault(l => l.Id == "foo")));
            Assert.That(actualRes.Created, Does.Contain(localSchedules.FirstOrDefault(l => l.Id == "bar")));
            Assert.That(actualRes.Created, Does.Contain(localSchedules.FirstOrDefault(l => l.Id == "dup-id")));
            Assert.That(actualRes.Deployed.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task DeployAsync_DuplicateNames()
    {
        var localSchedules = GetLocalConfigs();
        localSchedules.Add(new ScheduleConfig("dup-name",
            "EventType5",
            "recurring",
            "0 * * * *",
            1,
            "{}")
        {
            Id = "dup-id",
            Path = "otherpath.sched"
        });
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.DeployAsync(
            localSchedules,
            dryRun: true
        );
        Assert.Multiple(() =>
        {
            Assert.Contains(localSchedules.FirstOrDefault(l => l.Name == "dup-name"), actualRes.Failed);
            Assert.That(actualRes.Failed.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task DeployAsync_ExceptionWhenDeployingResource()
    {
        var localSchedules = GetLocalConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = new SchedulerDeploymentHandler(mockSchedulesClient.Object);

        mockSchedulesClient
            .Setup(c => c.List())
            .ReturnsAsync(new List<IScheduleConfig>());
        mockSchedulesClient
            .Setup(c => c.Create(It.IsAny<IScheduleConfig>()))
            .ThrowsAsync(new Exception());

        var actualRes = await handler.DeployAsync(
            localSchedules
        );

        Assert.That(actualRes.Failed.Count, Is.EqualTo(3));
        Assert.That(actualRes.Failed.First().Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    static List<IScheduleConfig> GetLocalConfigs()
    {
        var schedules = new List<IScheduleConfig>()
        {
            new ScheduleConfig("foo",
                "EventType1",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Id = "foo",
                Path = "path1"
            },
            new ScheduleConfig("bar",
                "EventType2",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Id = "bar",
                Path = "path2"
            },
            new ScheduleConfig("dup-name",
                "EventType4",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Id = "dup-id",
                Path = "path3"
            }
        };
        return schedules;
    }

    static IReadOnlyList<IScheduleConfig> GetRemoteConfigs()
    {
        var schedules = new List<IScheduleConfig>()
        {
            new ScheduleConfig("foo",
                "EventType1",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Id = "foo",
                Path = "Remote"
            },
            new ScheduleConfig("echo",
                "EventType3",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Id = "echo",
                Path = "Remote"
            }
        };
        return schedules;
    }
}
