using Moq;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
class SchedulerFetchHandlerTests
{
    [Test]
    public async Task FetchAsync_CorrectResult()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.FetchAsync("dir", localSchedules);

        Assert.Multiple(() =>
        {
            Assert.That(actualRes.Deployed, Does.Contain(localSchedules[0]));
            Assert.That(actualRes.Deployed, Does.Contain(localSchedules[1]));
            Assert.That(actualRes.Deployed, Does.Contain(localSchedules[2]));
            Assert.That(localSchedules[0].Status.MessageDetail, Is.EqualTo(Constants.Updated));
            Assert.That(localSchedules[1].Status.MessageDetail, Is.EqualTo(Constants.Deleted));
            Assert.That(localSchedules[2].Status.MessageDetail, Is.EqualTo(Constants.Deleted));
            Assert.That(actualRes.Deployed, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task FetchAsync_WriteCallsMade()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.FetchAsync("dir", localSchedules);

        mockResourceLoader.Verify(
            loader => loader.CreateOrUpdateResource(
                It.Is<SchedulerEntryDeploymentItem>(item =>
                    item.Path == "path1" && item.entry.Schedule == "1 * * * *"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockResourceLoader.Verify(
            loader => loader.CreateOrUpdateResource(
                It.Is<SchedulerEntryDeploymentItem>(item => item.entry.Name == "schedule4"),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_DeleteCallsMade()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.FetchAsync("dir", localSchedules);

        mockResourceLoader.Verify(
            loader => loader.DeleteResource(
                It.Is<SchedulerEntryDeploymentItem>(item => item.Path == "path2"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockResourceLoader.Verify(
            loader => loader.DeleteResource(
                It.Is<SchedulerEntryDeploymentItem>(item => item.Path == "path3"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_WriteNewOnReconcile()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules,
            reconcile: true);

        mockResourceLoader.Verify(
            loader => loader.CreateOrUpdateResource(
                It.Is<SchedulerEntryDeploymentItem>(item =>
                    item.Path == Path.Combine("dir", "schedule4.sched")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var created = actualRes.Deployed
            .Where(item => item.Status.MessageDetail == Constants.Created)
            .ToList();
        Assert.Multiple(() =>
        {
            Assert.That(created, Has.Count.EqualTo(1));
            Assert.That(created[0].entry.Name, Is.EqualTo("schedule4"));
        });
    }

    [Test]
    public async Task FetchAsync_StatusesAreCorrect()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules,
            reconcile: true);

        var expectedCreatedSchedule = actualRes.Deployed.FirstOrDefault(item => item.entry.Name == "schedule4");
        Assert.That(expectedCreatedSchedule?.Status.Message, Is.EqualTo("Fetched"));
        Assert.That(expectedCreatedSchedule?.Status.MessageDetail, Is.EqualTo(Constants.Created));

        var expectedUpdatedSchedule = actualRes.Deployed.FirstOrDefault(item => item.entry.Name == "schedule1");
        Assert.That(expectedUpdatedSchedule?.Status.Message, Is.EqualTo("Fetched"));
        Assert.That(expectedUpdatedSchedule?.Status.MessageDetail, Is.EqualTo(Constants.Updated));

        var expectedDeletedSchedule = actualRes.Deployed.FirstOrDefault(item => item.entry.Name == "schedule2");
        Assert.That(expectedDeletedSchedule?.Status.Message, Is.EqualTo("Fetched"));
        Assert.That(expectedDeletedSchedule?.Status.MessageDetail, Is.EqualTo(Constants.Deleted));
    }

    [Test]
    public async Task FetchAsync_DryRunNoCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        await handler.FetchAsync(
            "dir",
            localSchedules,
            dryRun: true);

        mockResourceLoader.Verify(
            loader => loader.DeleteResource(
                It.IsAny<SchedulerEntryDeploymentItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        mockResourceLoader.Verify(
            loader => loader.CreateOrUpdateResource(
                It.IsAny<SchedulerEntryDeploymentItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_DuplicateNames()
    {
        var localSchedules = GetLocalConfigs();
        localSchedules.Add(CreateItem("schedule1", "EventType1", "otherpath"));
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules,
            dryRun: true);

        mockResourceLoader.Verify(
            loader => loader.DeleteResource(
                It.Is<SchedulerEntryDeploymentItem>(item => item.Path == "path3"),
                It.IsAny<CancellationToken>()),
            Times.Never);
        var failed = actualRes.Deployed
            .Where(item => item.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();
        Assert.Multiple(() =>
        {
            Assert.That(failed[0].ToString(), Is.EqualTo("'schedule1' in 'path1'"));
            Assert.That(failed[1].ToString(), Is.EqualTo("'schedule1' in 'otherpath'"));
        });
    }

    [Test]
    public async Task FetchAsync_ExceptionWhenFetchingResource()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<ISchedulerResourceLoader> mockResourceLoader = new();
        var handler = CreateHandler(mockSchedulerClient, mockResourceLoader);

        mockSchedulerClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(remoteSchedules);
        mockResourceLoader
            .Setup(loader => loader.CreateOrUpdateResource(
                It.IsAny<SchedulerEntryDeploymentItem>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var actualRes = await handler.FetchAsync("dir", localSchedules);

        var failed = actualRes.Deployed
            .Where(item => item.Status.MessageSeverity == SeverityLevel.Error)
            .ToList();
        Assert.Multiple(() =>
        {
            Assert.That(failed, Has.Count.EqualTo(1));
            Assert.That(failed[0].Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
        });
    }

    static SchedulerFetchHandler CreateHandler(
        Mock<ISchedulerClient> client,
        Mock<ISchedulerResourceLoader> resourceLoader)
    {
        return new SchedulerFetchHandler(
            client.Object,
            resourceLoader.Object,
            Mock.Of<Tooling.Editor.Scheduler.Authoring.Core.Logger.ILogger>());
    }

    static List<SchedulerEntryDeploymentItem> GetLocalConfigs()
    {
        return new List<SchedulerEntryDeploymentItem>
        {
            CreateItem("schedule1", "EventType1", "path1"),
            CreateItem("schedule2", "EventType1", "path2"),
            CreateItem("schedule3", "EventType1", "path3")
        };
    }

    static IReadOnlyList<SchedulerEntry> GetRemoteConfigs()
    {
        return new List<SchedulerEntry>
        {
            CreateEntry("schedule1", "EventType1", "1 * * * *"),
            CreateEntry("schedule4", "EventType1", "0 * * * *")
        };
    }

    static SchedulerEntryDeploymentItem CreateItem(string name, string eventName, string path)
    {
        return new SchedulerEntryDeploymentItem(path)
        {
            Name = name,
            entry = CreateEntry(name, eventName, "0 * * * *")
        };
    }

    static SchedulerEntry CreateEntry(string name, string eventName, string schedule)
    {
        return new SchedulerEntry
        {
            Name = name,
            EventName = eventName,
            ScheduleType = "recurring",
            Schedule = schedule,
            PayloadVersion = 1,
            Payload = "{}"
        };
    }
}
