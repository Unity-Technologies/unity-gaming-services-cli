using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.Cli.Scheduler.Fetch;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class SchedulerFetchServiceTests
{
    SchedulerFetchService? m_FetchService;
    readonly Mock<ISchedulerClient> m_MockScheduleClient = new();
    readonly Mock<IScheduleFetchHandler> m_MockScheduleFetchHandler = new();
    readonly Mock<IScheduleResourceLoader> m_MockScheduleConfigLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_MockScheduleClient.Reset();
        m_FetchService = new SchedulerFetchService(
            m_MockScheduleFetchHandler.Object,
            m_MockScheduleClient.Object,
            m_MockScheduleConfigLoader.Object);
    }

    [Test]
    public async Task FetchAsync_MapsResult()
    {
        var schedule1 = new ScheduleConfig(
            "schedule1",
            "EventType1",
            "recurring",
            "0 * * * *",
            1,
            "{}")
        {
            Id = "schedule1",
            Path = "scheduleFile.sched",
        };
        var schedule2 = new ScheduleConfig(
            "schedule2",
            "EventType1",
            "recurring",
            "0 * * * *",
            1,
            "{}")
        {
            Id = "schedule2",
            Path = "scheduleFile.sched",
        };
        m_MockScheduleConfigLoader
            .Setup(
                m =>
                    m.LoadResource(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ScheduleFileItem(new ScheduleConfigFile(
                new Dictionary<string, ScheduleConfig>()
                {
                    { "schedule1", schedule1 },
                    { "schedule2", schedule2 }
                }), "scheduleFile.sched"));
        var deployResult = new FetchResult()
        {
            Created = new List<IScheduleConfig> { schedule2 },
            Updated = new List<IScheduleConfig>(),
            Deleted = new List<IScheduleConfig>(),
            Fetched = new List<IScheduleConfig> { schedule2 },
            Failed = new List<IScheduleConfig>()
        };
        m_MockScheduleFetchHandler.Setup(
                d => d.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<IScheduleConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult(deployResult));

        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
            input,
            new[] { "dir" },
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(res.Created.Count, Is.EqualTo(1));
            Assert.That(res.Updated.Count, Is.EqualTo(0));
            Assert.That(res.Deleted.Count, Is.EqualTo(0));
            Assert.That(res.Fetched.Count, Is.EqualTo(2));
            Assert.That(res.Failed.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task FetchAsync_MapsFailed()
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
        var deployResult = new FetchResult()
        {
            Created = new List<IScheduleConfig>(),
            Updated = new List<IScheduleConfig>(),
            Deleted = new List<IScheduleConfig>(),
            Fetched = new List<IScheduleConfig>(),
            Failed = new List<IScheduleConfig>()
        };
        m_MockScheduleFetchHandler.Setup(
                d => d.FetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<IScheduleConfig>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult(deployResult));

        var input = new FetchInput()
        {
            Path = "dir",
            CloudProjectId = string.Empty
        };
        var res = await m_FetchService!.FetchAsync(
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
            Assert.That(res.Fetched.Count, Is.EqualTo(0));
            Assert.That(res.Failed.Count, Is.EqualTo(1));
        });
    }
}
