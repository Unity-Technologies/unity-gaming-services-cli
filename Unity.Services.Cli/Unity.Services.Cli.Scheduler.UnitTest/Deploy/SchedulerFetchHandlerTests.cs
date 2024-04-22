using Moq;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Scheduler.Authoring.Core.IO;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;

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
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules
        );

        Assert.Contains(localSchedules.FirstOrDefault(l => l.Name == "schedule1"), actualRes.Updated);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Name == "schedule1"), actualRes.Fetched);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Name == "schedule2"), actualRes.Deleted);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Name == "schedule2"), actualRes.Fetched);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Name == "schedule3"), actualRes.Deleted);
        Assert.Contains(localSchedules.FirstOrDefault(l => l.Name == "schedule3"), actualRes.Fetched);
        Assert.IsEmpty(actualRes.Created);
    }

    [Test]
    public async Task FetchAsync_WriteCallsMade()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules
        );

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    "path1",
                    It.Is<string>(s => s.Contains("1 * * * *")),
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
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules
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
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules,
            reconcile: true
        );

        mockFileSystem
            .Verify(f => f.WriteAllText(
                    Path.Combine("dir", "schedule4.sched"),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        Assert.That(actualRes.Created.Count, Is.EqualTo(1));
        Assert.That(actualRes.Created.First().Name, Is.EqualTo("schedule4"));
    }


    [Test]
    public async Task FetchAsync_StatusesAreCorrect()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules,
            reconcile: true
        );

        var expectedCreatedSchedule = actualRes.Fetched.FirstOrDefault(l => l.Name == "schedule4");
        Assert.IsTrue(expectedCreatedSchedule?.Status.Message == "Fetched");
        Assert.IsTrue(expectedCreatedSchedule?.Status.MessageDetail == "Created");

        var expectedUpdatedSchedule = actualRes.Fetched.FirstOrDefault(l => l.Name == "schedule1");
        Assert.IsTrue(expectedUpdatedSchedule?.Status.Message == "Fetched");
        Assert.IsTrue(expectedUpdatedSchedule?.Status.MessageDetail == "Updated");

        var expectedDeletedSchedule = actualRes.Fetched.FirstOrDefault(l => l.Name == "schedule2");
        Assert.IsTrue(expectedDeletedSchedule?.Status.Message == "Fetched");
        Assert.IsTrue(expectedDeletedSchedule?.Status.MessageDetail == "Deleted");
    }



    [Test]
    public async Task FetchAsync_DryRunNoCalls()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules,
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
    public async Task FetchAsync_DuplicateNames()
    {
        var localSchedules = GetLocalConfigs();
        localSchedules.Add(new ScheduleConfig("schedule1",
            "EventType1",
            "recurring",
            "0 * * * *",
            1,
            "{}")
        { Path = "otherpath" });
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules,
            dryRun: true
        );

        mockFileSystem
            .Verify(f => f.Delete(
                    It.Is<string>(s => s == "path3"),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        Assert.Multiple(() =>
        {
            Assert.That(actualRes.Failed[0].ToString(), Is.EqualTo("'schedule1' in 'path1'"));
            Assert.That(actualRes.Failed[1].ToString(), Is.EqualTo("'schedule1' in 'otherpath'"));
        });
    }

    [Test]
    public async Task FetchAsync_ExceptionWhenFetchingResource()
    {
        var localSchedules = GetLocalConfigs();
        var remoteSchedules = GetRemoteConfigs();

        Mock<ISchedulerClient> mockSchedulerClient = new();
        Mock<IFileSystem> mockFileSystem = new();
        var handler = new SchedulerFetchHandler(mockSchedulerClient.Object, mockFileSystem.Object, new SchedulesSerializer());

        mockSchedulerClient
            .Setup(c => c.List())
            .ReturnsAsync(remoteSchedules.ToList());
        mockFileSystem.Setup(c => c.WriteAllText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var actualRes = await handler.FetchAsync(
            "dir",
            localSchedules
        );

        Assert.That(actualRes.Failed.Count, Is.EqualTo(1));
        Assert.That(actualRes.Failed.First().Status.MessageSeverity, Is.EqualTo(SeverityLevel.Error));
    }

    static List<IScheduleConfig> GetLocalConfigs()
    {
        var schedules = new List<IScheduleConfig>()
        {
            new ScheduleConfig("schedule1",
                "EventType1",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Path = "path1"
            },
            new ScheduleConfig("schedule2",
                "EventType1",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Path = "path2"
            },
            new ScheduleConfig("schedule3",
                "EventType1",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Path = "path3"
            }
        };
        return schedules;
    }

    static List<IScheduleConfig> GetRemoteConfigs()
    {
        var schedules = new List<IScheduleConfig>()
        {
            new ScheduleConfig("schedule1",
                "EventType1",
                "recurring",
                "1 * * * *",
                1,
                "{}")
            {
                Path = "Remote"
            },
            new ScheduleConfig("schedule4",
                "EventType1",
                "recurring",
                "0 * * * *",
                1,
                "{}")
            {
                Path = "Remote"
            }
        };
        return schedules;
    }
}
