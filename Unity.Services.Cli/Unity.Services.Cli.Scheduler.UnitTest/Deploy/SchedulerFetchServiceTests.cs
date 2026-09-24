using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Scheduler.Fetch;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;
using CoreDeploymentResult = Unity.Services.DeploymentApi.Editor.DeploymentResult<Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model.SchedulerEntryDeploymentItem>;
using Statuses = Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model.Statuses;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class SchedulerFetchServiceTests
{
    SchedulerFetchService? m_FetchService;
    readonly Mock<ISchedulerClient> m_MockScheduleClient = new();
    readonly Mock<ISchedulerFetchHandler> m_MockScheduleFetchHandler = new();
    readonly Mock<ISchedulerResourceLoader> m_MockScheduleConfigLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockScheduleClient.Reset();
        m_MockScheduleFetchHandler.Reset();
        m_MockScheduleConfigLoader.Reset();
        m_FetchService = new SchedulerFetchService(
            m_MockScheduleFetchHandler.Object,
            m_MockScheduleClient.Object,
            m_MockScheduleConfigLoader.Object);
    }

    [Test]
    public async Task FetchAsync_MapsResult()
    {
        var schedule1 = new SchedulerEntry
        {
            Id = "schedule1",
            Name = "schedule1",
            EventName = "EventType1",
            ScheduleType = "recurring",
            Schedule = "0 * * * *",
            PayloadVersion = 1,
            Payload = "{}"
        };
        var schedule2 = new SchedulerEntry
        {
            Id = "schedule2",
            Name = "schedule2",
            EventName = "EventType1",
            ScheduleType = "recurring",
            Schedule = "0 * * * *",
            PayloadVersion = 1,
            Payload = "{}"
        };
        var files = new List<SchedulerEntryDeploymentItem>
        {
            new("scheduleFile.sched")
            {
                Name = schedule1.Name,
                entry = schedule1,
                Status = Statuses.GetFetched(Constants.Updated)
            },
            new("scheduleFile.sched")
            {
                Name = schedule2.Name,
                entry = schedule2,
                Status = Statuses.GetFetched(Constants.Created)
            }
        };
        m_MockScheduleConfigLoader
            .Setup(
                m =>
                    m.ReadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(files);
        var fetchResult = new CoreDeploymentResult(files);
        m_MockScheduleFetchHandler.Setup(
                d => d.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult(fetchResult));

        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
            input,
            [new AuthoringFile("dir")],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(res.Created.Count, Is.EqualTo(1));
            Assert.That(res.Updated.Count, Is.EqualTo(1));
            Assert.That(res.Deleted.Count, Is.EqualTo(0));
            Assert.That(res.Fetched.Count, Is.EqualTo(1));
            Assert.That(res.Failed.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task FetchAsync_MapsFailed()
    {
        var failedItem = new SchedulerEntryDeploymentItem("scheduleFile.sched")
        {
            Status = new DeploymentStatus("failed", "failed", SeverityLevel.Error)
        };
        m_MockScheduleConfigLoader
            .Setup(
                m =>
                    m.ReadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([failedItem]);
        var fetchResult = new CoreDeploymentResult();
        m_MockScheduleFetchHandler.Setup(
                d => d.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult(fetchResult));

        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
            input,
            [new AuthoringFile("dir")],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(res.Created.Count, Is.EqualTo(0));
            Assert.That(res.Updated.Count, Is.EqualTo(0));
            Assert.That(res.Deleted.Count, Is.EqualTo(0));
            Assert.That(res.Fetched.Count, Is.EqualTo(0));
            Assert.That(res.Failed.Count, Is.EqualTo(2));
        });
    }
}
