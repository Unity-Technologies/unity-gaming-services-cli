using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.Cli.Scheduler.Fetch;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;
using CoreDeploymentResult = Unity.Services.DeploymentApi.Editor.DeploymentResult<Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model.SchedulerEntryDeploymentItem>;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class SchedulerDryRunOutputTests
{
    readonly Mock<ISchedulerClient> m_Client = new();
    readonly Mock<ISchedulerResourceLoader> m_ResourceLoader = new();

    [SetUp]
    public void SetUp()
    {
        m_Client.Reset();
        m_ResourceLoader.Reset();
        m_Client
            .Setup(client => client.Initialize(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task DeployDryRun_DisplaysFileOnceAndScheduleNames()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "new_file.sched");
        var schedule1 = CreateItem("Schedule1", path, Constants.Updated);
        var schedule2 = CreateItem("Schedule2", path, Constants.Created);
        var schedule3 = CreateItem("Schedule3", path, Constants.Updated);
        var deployed = new[] { schedule1, schedule2, schedule3 };

        m_ResourceLoader
            .Setup(loader => loader.ReadResource(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployed.ToList());
        var handler = new Mock<ISchedulerDeploymentHandler>();
        handler
            .Setup(value => value.DeployAsync(
                It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoreDeploymentResult(deployed));

        var service = new SchedulerDeploymentService(
            handler.Object,
            m_Client.Object,
            m_ResourceLoader.Object);

        var result = await service.Deploy(
            new DeployInput { DryRun = true },
            [new AuthoringFile(path)],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        var table = result.ToTable(SchedulerConstants.ServiceType);

        Assert.Multiple(() =>
        {
            Assert.That(result.Deployed, Has.Count.EqualTo(1));
            Assert.That(result.Deployed.Single().Path, Is.EqualTo($".{Path.DirectorySeparatorChar}new_file.sched"));
            Assert.That(result.Updated.Select(item => item.ToString()), Is.EquivalentTo(new[]
            {
                $"'Schedule1' in '.{Path.DirectorySeparatorChar}new_file.sched'",
                $"'Schedule3' in '.{Path.DirectorySeparatorChar}new_file.sched'"
            }));
            Assert.That(result.Created.Single().ToString(),
                Is.EqualTo($"'Schedule2' in '.{Path.DirectorySeparatorChar}new_file.sched'"));
            Assert.That(table.Result.Select(row => row.Name),
                Is.EqualTo(new[] { "new_file.sched", "Schedule1", "Schedule3", "Schedule2" }));
            Assert.That(table.Result.Select(row => row.Service),
                Is.All.EqualTo(SchedulerConstants.ServiceType));
        });
    }

    [Test]
    public async Task DeployDryRun_IdLessSchedulesWithUniqueNamesAreNotDuplicates()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "new_file.sched");
        var schedules = new[]
        {
            CreateItem("Schedule1", path, Constants.Created),
            CreateItem("Schedule2", path, Constants.Created),
            CreateItem("Schedule3", path, Constants.Created)
        };
        foreach (var schedule in schedules)
            schedule.entry.Id = null;
        var handler = new SchedulerDeploymentHandler(
            m_Client.Object,
            Mock.Of<Tooling.Editor.Scheduler.Authoring.Core.Logger.ILogger>());
        m_ResourceLoader
            .Setup(loader => loader.ReadResource(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules.ToList());
        m_Client
            .Setup(client => client.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SchedulerEntry>());
        var service = new SchedulerDeploymentService(
            handler,
            m_Client.Object,
            m_ResourceLoader.Object);

        var result = await service.Deploy(
            new DeployInput { DryRun = true },
            [new AuthoringFile(path)],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.Empty);
            Assert.That(result.Created.Select(item => item.Name),
                Is.EquivalentTo(new[] { "Schedule1", "Schedule2", "Schedule3" }));
        });
    }

    [Test]
    public async Task FetchDryRun_DisplaysEachFileOnceAndScheduleNames()
    {
        var existingPath = Path.Combine(Directory.GetCurrentDirectory(), "new_file.sched");
        var createdPath = Path.Combine(Directory.GetCurrentDirectory(), "TurnOnRule.sched");
        var schedule1 = CreateItem("Schedule1", existingPath, Constants.Updated);
        var schedule3 = CreateItem("Schedule3", existingPath, Constants.Updated);
        var turnOnRule = CreateItem("TurnOnRule", createdPath, Constants.Created);
        turnOnRule.Name = "TurnOnRule.sched";
        var fetched = new[] { schedule1, schedule3, turnOnRule };

        m_ResourceLoader
            .Setup(loader => loader.ReadResource(existingPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchedulerEntryDeploymentItem> { schedule1, schedule3 });
        var handler = new Mock<ISchedulerFetchHandler>();
        handler
            .Setup(value => value.FetchAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
                true,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoreDeploymentResult(fetched));

        var service = new SchedulerFetchService(
            handler.Object,
            m_Client.Object,
            m_ResourceLoader.Object);

        var result = await service.FetchAsync(
            new FetchInput { Path = "/project", DryRun = true, Reconcile = true },
            [new AuthoringFile(existingPath)],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        var table = result.ToTable(SchedulerConstants.ServiceType);

        Assert.Multiple(() =>
        {
            Assert.That(result.Fetched.Select(item => item.Path), Is.EquivalentTo(new[]
            {
                $".{Path.DirectorySeparatorChar}new_file.sched",
                $".{Path.DirectorySeparatorChar}TurnOnRule.sched"
            }));
            Assert.That(result.Updated.Select(item => item.ToString()), Is.EquivalentTo(new[]
            {
                $"'Schedule1' in '.{Path.DirectorySeparatorChar}new_file.sched'",
                $"'Schedule3' in '.{Path.DirectorySeparatorChar}new_file.sched'"
            }));
            Assert.That(result.Created.Single().ToString(),
                Is.EqualTo($"'TurnOnRule' in '.{Path.DirectorySeparatorChar}TurnOnRule.sched'"));
            Assert.That(table.Result.Select(row => row.Name), Is.EqualTo(new[]
            {
                "new_file.sched",
                "Schedule1",
                "Schedule3",
                "TurnOnRule.sched",
                "TurnOnRule.sched"
            }));
            Assert.That(table.Result.Select(row => row.Type), Is.EqualTo(new[]
            {
                "Schedule Config File",
                "Schedule",
                "Schedule",
                "Schedule Config File",
                "Schedule"
            }));
            Assert.That(table.Result.Select(row => row.Service),
                Is.All.EqualTo(SchedulerConstants.ServiceType));
        });
    }

    [Test]
    public async Task FetchDryRun_GroupsDuplicateFailuresByFile()
    {
        var firstPath = Path.Combine(Directory.GetCurrentDirectory(), "new_file.sched");
        var secondPath = Path.Combine(Directory.GetCurrentDirectory(), "out.sched");
        var firstSchedule1 = CreateDuplicateItem("Schedule1", firstPath, secondPath);
        var duplicateFirstSchedule1 = CreateDuplicateItem("Schedule1", firstPath, secondPath);
        var firstSchedule2 = CreateDuplicateItem("Schedule2", firstPath, secondPath);
        var secondSchedule1 = CreateDuplicateItem("Schedule1", secondPath, firstPath);
        var secondSchedule2 = CreateDuplicateItem("Schedule2", secondPath, firstPath);
        var duplicates = new[]
        {
            firstSchedule1,
            duplicateFirstSchedule1,
            firstSchedule2,
            secondSchedule1,
            secondSchedule2
        };

        m_ResourceLoader
            .Setup(loader => loader.ReadResource(firstPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchedulerEntryDeploymentItem>
            {
                firstSchedule1,
                duplicateFirstSchedule1,
                firstSchedule2
            });
        m_ResourceLoader
            .Setup(loader => loader.ReadResource(secondPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchedulerEntryDeploymentItem> { secondSchedule1, secondSchedule2 });
        var handler = new Mock<ISchedulerFetchHandler>();
        handler
            .Setup(value => value.FetchAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
                true,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoreDeploymentResult(duplicates));

        var service = new SchedulerFetchService(
            handler.Object,
            m_Client.Object,
            m_ResourceLoader.Object);

        var result = await service.FetchAsync(
            new FetchInput { Path = Directory.GetCurrentDirectory(), DryRun = true, Reconcile = true },
            [new AuthoringFile(firstPath), new AuthoringFile(secondPath)],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        var output = result.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Has.Count.EqualTo(2));
            var firstFailure = result.Failed.Single(item => item.Path.EndsWith("new_file.sched", StringComparison.Ordinal));
            Assert.That(firstSchedule1.Path, Is.EqualTo(firstPath));
            Assert.That(secondSchedule1.Path, Is.EqualTo(secondPath));
            Assert.That(firstFailure.Status.MessageDetail.Split("Schedule1").Length - 1, Is.EqualTo(1));
            Assert.That(output, Does.Contain("Duplicate identifiers:"));
            Assert.That(output, Does.Contain($"Schedule1 (also in '.{Path.DirectorySeparatorChar}out.sched')"));
            Assert.That(output, Does.Contain($"Schedule2 (also in '.{Path.DirectorySeparatorChar}out.sched')"));
            Assert.That(output, Does.Contain($"Schedule1 (also in '.{Path.DirectorySeparatorChar}new_file.sched')"));
            Assert.That(output, Does.Not.Contain("All items failed to fetch"));
            Assert.That(output, Does.Not.Contain(Directory.GetCurrentDirectory()));
        });
    }

    [Test]
    public async Task DeployDryRun_GroupsIdLessDuplicateNamesAcrossFiles()
    {
        var firstPath = Path.Combine(Directory.GetCurrentDirectory(), "new_file.sched");
        var secondPath = Path.Combine(Directory.GetCurrentDirectory(), "out.sched");
        var firstSchedule = CreateItem("Schedule1", firstPath, Constants.Created);
        var secondSchedule = CreateItem("Schedule1", secondPath, Constants.Created);
        firstSchedule.entry.Id = null;
        secondSchedule.entry.Id = null;
        var handler = new SchedulerDeploymentHandler(
            m_Client.Object,
            Mock.Of<Tooling.Editor.Scheduler.Authoring.Core.Logger.ILogger>());
        m_ResourceLoader
            .Setup(loader => loader.ReadResource(firstPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchedulerEntryDeploymentItem> { firstSchedule });
        m_ResourceLoader
            .Setup(loader => loader.ReadResource(secondPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchedulerEntryDeploymentItem> { secondSchedule });
        m_Client
            .Setup(client => client.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SchedulerEntry>());
        var service = new SchedulerDeploymentService(
            handler,
            m_Client.Object,
            m_ResourceLoader.Object);

        var result = await service.Deploy(
            new DeployInput { DryRun = true },
            [new AuthoringFile(firstPath), new AuthoringFile(secondPath)],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        var output = result.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Has.Count.EqualTo(2));
            Assert.That(output, Does.Contain("Schedule1"));
            Assert.That(output, Does.Contain($"also in '.{Path.DirectorySeparatorChar}out.sched'"));
            Assert.That(output, Does.Not.Contain("same identifier ''"));
            Assert.That(output, Does.Not.Contain("All items failed to deploy"));
            Assert.That(output, Does.Not.Contain(Directory.GetCurrentDirectory()));
        });
    }

    [Test]
    public async Task DeployDryRun_GroupsDuplicateNamesWithinOneFile()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "new_file.sched");
        var firstSchedule = CreateItem("Schedule1", path, Constants.Created);
        var secondSchedule = CreateItem("Schedule1", path, Constants.Created);
        firstSchedule.entry.Id = null;
        secondSchedule.entry.Id = null;
        var handler = new SchedulerDeploymentHandler(
            m_Client.Object,
            Mock.Of<Tooling.Editor.Scheduler.Authoring.Core.Logger.ILogger>());
        m_ResourceLoader
            .Setup(loader => loader.ReadResource(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchedulerEntryDeploymentItem> { firstSchedule, secondSchedule });
        m_Client
            .Setup(client => client.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SchedulerEntry>());
        var service = new SchedulerDeploymentService(
            handler,
            m_Client.Object,
            m_ResourceLoader.Object);

        var result = await service.Deploy(
            new DeployInput { DryRun = true },
            [new AuthoringFile(path)],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);
        var output = result.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Has.Count.EqualTo(1));
            Assert.That(result.Failed.Single().Status.Message, Is.EqualTo("Duplicate schedules"));
            Assert.That(result.Failed.Single().Status.MessageDetail, Does.Contain("  Schedule1"));
            Assert.That(result.Failed.Single().Status.MessageDetail, Does.Not.Contain("also in"));
            Assert.That(output, Does.Not.Contain("Multiple resources with the same identifier"));
            Assert.That(output, Does.Not.Contain(Directory.GetCurrentDirectory()));
        });
    }

    [Test]
    public async Task Deploy_DisplaysFileOnceAndScheduleNames()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "new_file.sched");
        var schedule1 = CreateItem("Schedule1", path, Constants.Updated);
        var schedule2 = CreateItem("Schedule2", path, Constants.Created);
        var deployed = new[] { schedule1, schedule2 };

        m_ResourceLoader
            .Setup(loader => loader.ReadResource(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deployed.ToList());
        var handler = new Mock<ISchedulerDeploymentHandler>();
        handler
            .Setup(value => value.DeployAsync(
                It.IsAny<IReadOnlyList<SchedulerEntryDeploymentItem>>(),
                false,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoreDeploymentResult(deployed));

        var service = new SchedulerDeploymentService(
            handler.Object,
            m_Client.Object,
            m_ResourceLoader.Object);

        var result = await service.Deploy(
            new DeployInput(),
            [new AuthoringFile(path)],
            string.Empty,
            string.Empty,
            null,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Deployed, Has.Count.EqualTo(1));
            Assert.That(result.Updated.Single().ToString(),
                Is.EqualTo($"'Schedule1' in '.{Path.DirectorySeparatorChar}new_file.sched'"));
            Assert.That(result.Created.Single().ToString(),
                Is.EqualTo($"'Schedule2' in '.{Path.DirectorySeparatorChar}new_file.sched'"));
        });
    }

    [Test]
    public void SchedulerEntryDeploymentItem_DisplaysRemoteNameAndPath()
    {
        var item = CreateItem("Schedule1", "Remote", Constants.Deleted);

        Assert.That(item.ToString(), Is.EqualTo("'Schedule1' in 'Remote'"));
    }

    static SchedulerEntryDeploymentItem CreateItem(string name, string path, string action)
    {
        return new SchedulerEntryDeploymentItem(path)
        {
            Name = name,
            entry = new SchedulerEntry
            {
                Id = name,
                Name = name,
                EventName = $"event.{name}"
            },
            Status = Tooling.Editor.Scheduler.Authoring.Core.Model.Statuses.GetDeployed(action)
        };
    }

    static SchedulerEntryDeploymentItem CreateDuplicateItem(string name, string path, string duplicatePath)
    {
        var item = CreateItem(name, path, Constants.Updated);
        item.entry.Id = null;
        item.Status = Tooling.Editor.Scheduler.Authoring.Core.Model.Statuses.GetFailedToFetch(
            $"Multiple resources with the same identifier '{name}' were found. "
            + "Only a single resource for a given identifier may be deployed/fetched at the same time. "
            + "Give all resources unique identifiers or deploy/fetch them separately to proceed."
            + Environment.NewLine
            + $"'{path}' was found duplicated in other files: '{duplicatePath}'");
        return item;
    }
}
