using Moq;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;

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
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.DeployAsync(localSchedules);

        Assert.Multiple(() =>
        {
            Assert.That(actualRes.Deployed, Does.Contain(localSchedules[0]));
            Assert.That(actualRes.Deployed, Does.Contain(localSchedules[1]));
            Assert.That(actualRes.Deployed, Does.Contain(localSchedules[2]));
            Assert.That(localSchedules[0].Status.MessageDetail, Is.EqualTo(Constants.Updated));
            Assert.That(localSchedules[1].Status.MessageDetail, Is.EqualTo(Constants.Created));
            Assert.That(localSchedules[2].Status.MessageDetail, Is.EqualTo(Constants.Created));
        });
    }

    [Test]
    public async Task DeployAsync_CreateCallsMade()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.DeployAsync(localSchedules);

        mockSchedulesClient.Verify(
            c => c.Create(
                It.Is<SchedulerEntry>(entry => entry.Id == "bar"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockSchedulesClient.Verify(
            c => c.Create(
                It.Is<SchedulerEntry>(entry => entry.Id == "dup-id"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_UpdateCallsMade()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.DeployAsync(localSchedules);

        mockSchedulesClient.Verify(
            c => c.Update(
                It.Is<SchedulerEntry>(entry => entry.Id == "foo"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_StatusesSet()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.DeployAsync(
            localSchedules,
            reconcile: true);

        mockSchedulesClient.Verify(
            c => c.Update(
                It.Is<SchedulerEntry>(entry => entry.Id == "foo"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        var expectedCreatedSchedule = actualRes.Deployed.FirstOrDefault(item => item.entry.Id == "bar");
        Assert.That(expectedCreatedSchedule?.Status.Message, Is.EqualTo("Deployed"));
        Assert.That(expectedCreatedSchedule?.Status.MessageDetail, Is.EqualTo(Constants.Created));

        var expectedUpdatedSchedule = actualRes.Deployed.FirstOrDefault(item => item.entry.Id == "foo");
        Assert.That(expectedUpdatedSchedule?.Status.Message, Is.EqualTo("Deployed"));
        Assert.That(expectedUpdatedSchedule?.Status.MessageDetail, Is.EqualTo(Constants.Updated));

        var expectedDeletedSchedule = actualRes.Deployed.FirstOrDefault(item => item.entry.Id == "echo");
        Assert.That(expectedDeletedSchedule?.Status.Message, Is.EqualTo("Deployed"));
        Assert.That(expectedDeletedSchedule?.Status.MessageDetail, Is.EqualTo(Constants.Deleted));
    }

    [Test]
    public async Task DeployAsync_NoReconcileNoDeleteCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.DeployAsync(localSchedules);

        mockSchedulesClient.Verify(
            c => c.Delete(
                It.Is<SchedulerEntry>(entry => entry.Id == "echo"),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeployAsync_ReconcileDeleteCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.DeployAsync(
            localSchedules,
            reconcile: true);

        mockSchedulesClient.Verify(
            c => c.Delete(
                It.Is<SchedulerEntry>(entry => entry.Id == "echo"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_DryRunNoCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.DeployAsync(
            localSchedules,
            true);

        mockSchedulesClient.Verify(
            c => c.Create(It.IsAny<SchedulerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockSchedulesClient.Verify(
            c => c.Update(It.IsAny<SchedulerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockSchedulesClient.Verify(
            c => c.Delete(It.IsAny<SchedulerEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeployAsync_DryRunCorrectResult()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.DeployAsync(
            localSchedules,
            dryRun: true);

        Assert.Multiple(() =>
        {
            Assert.That(actualRes.Deployed, Has.Count.EqualTo(3));
            Assert.That(localSchedules[0].Status.MessageDetail, Is.EqualTo(Constants.Updated));
            Assert.That(localSchedules[1].Status.MessageDetail, Is.EqualTo(Constants.Created));
            Assert.That(localSchedules[2].Status.MessageDetail, Is.EqualTo(Constants.Created));
        });
    }

    [Test]
    public async Task DeployAsync_DuplicateNames()
    {
        var localSchedules = GetLocalConfigs();
        localSchedules.Add(CreateItem(
            "dup-id",
            "dup-name",
            "EventType5",
            "otherpath.sched"));
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.DeployAsync(
            localSchedules,
            dryRun: true);

        var failed = actualRes.Deployed
            .Where(item => item.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();
        Assert.Multiple(() =>
        {
            Assert.That(failed, Does.Contain(localSchedules.First(item => item.entry.Name == "dup-name")));
            Assert.That(failed, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task DeployAsync_ExceptionWhenDeployingResource()
    {
        var localSchedules = GetLocalConfigs();

        Mock<ISchedulerClient> mockSchedulesClient = new();
        var handler = CreateHandler(mockSchedulesClient);

        mockSchedulesClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SchedulerEntry>());
        mockSchedulesClient
            .Setup(c => c.Create(It.IsAny<SchedulerEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var actualRes = await handler.DeployAsync(localSchedules);

        var failed = actualRes.Deployed
            .Where(item => item.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();
        Assert.That(failed, Has.Count.EqualTo(3));
    }

    static SchedulerDeploymentHandler CreateHandler(Mock<ISchedulerClient> client)
    {
        return new SchedulerDeploymentHandler(
            client.Object,
            Mock.Of<Tooling.Editor.Scheduler.Authoring.Core.Logger.ILogger>());
    }

    static List<SchedulerEntryDeploymentItem> GetLocalConfigs()
    {
        return new List<SchedulerEntryDeploymentItem>
        {
            CreateItem("foo", "foo", "EventType1", "path1"),
            CreateItem("bar", "bar", "EventType2", "path2"),
            CreateItem("dup-id", "dup-name", "EventType4", "path3")
        };
    }

    static IReadOnlyList<SchedulerEntry> GetRemoteConfigs()
    {
        return new List<SchedulerEntry>
        {
            CreateEntry("foo", "foo", "EventType1"),
            CreateEntry("echo", "echo", "EventType3")
        };
    }

    static SchedulerEntryDeploymentItem CreateItem(
        string id,
        string name,
        string eventName,
        string path)
    {
        return new SchedulerEntryDeploymentItem(path)
        {
            Name = name,
            entry = CreateEntry(id, name, eventName)
        };
    }

    static SchedulerEntry CreateEntry(string id, string name, string eventName)
    {
        return new SchedulerEntry
        {
            Id = id,
            Name = name,
            EventName = eventName,
            ScheduleType = "recurring",
            Schedule = "0 * * * *",
            PayloadVersion = 1,
            Payload = "{}"
        };
    }
}
