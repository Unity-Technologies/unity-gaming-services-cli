using Moq;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.Cli.Scheduler.UnitTest.Utils;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Client;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Api;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Model;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;
using Configuration = Unity.Services.Gateway.SchedulerApiV1.Generated.Client.Configuration;

namespace Unity.Services.Cli.Scheduler.UnitTest.Deploy;

[TestFixture]
public class SchedulerClientTests
{
    readonly ScheduleConfig m_Schedule;
    Mock<ISchedulerApiAsync> m_MockApi;
    Mock<IServiceAccountAuthenticationService> m_MockAuthService;
    Mock<IConfigurationValidator> m_MockValidator;
    SchedulerClient m_SchedulerClient;

    public SchedulerClientTests()
    {
        m_Schedule = new ScheduleConfig(new Guid("00000000-0000-0000-0000-000000000001"),
            "foo",
            "EventType1",
            "recurring",
            "0 * * * *",
            1,
            "{}");
        m_Schedule.StartAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        m_Schedule.EndAt = new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc);
    }

    [SetUp]
    public void Setup()
    {
        m_MockApi = new Mock<ISchedulerApiAsync>();
        m_MockApi.Setup(a => a.Configuration)
            .Returns(new Configuration());
        m_MockAuthService = new Mock<IServiceAccountAuthenticationService>();
        m_MockValidator = new Mock<IConfigurationValidator>();
        m_SchedulerClient = new SchedulerClient(m_MockApi.Object, m_MockAuthService.Object, m_MockValidator.Object);
    }

    [Test]
    public async Task Initialize_Succeed()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        Assert.That(m_SchedulerClient.EnvironmentId.ToString(), Is.EqualTo(TestValues.ValidEnvironmentId));
        Assert.That(m_SchedulerClient.ProjectId.ToString(), Is.EqualTo(TestValues.ValidProjectId));
    }

    [Test]
    public async Task ListMoreThanLimit()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var schedules = Enumerable.Range(0, 75)
            .Select(
                i => new ScheduleConfig(
                    Guid.NewGuid(),
                    "name" + i,
                    "event" + i,
                    m_Schedule.Type,
                    m_Schedule.Schedule,
                    m_Schedule.PayloadVersion,
                    m_Schedule.Payload)).ToList();
        m_MockApi.Setup(
            api => api.ListSchedulerConfigsAsync(
                It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
                It.Is<int>(i => i == 50),
                It.Is<string>(s => s == null),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(
                    new SchedulerConfigPage(
                        null!,
                        schedules.Take(50).ToList())));
        m_MockApi.Setup(
                api => api.ListSchedulerConfigsAsync(
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
                    It.Is<int>(i => i == 50),
                    It.Is<string>(s => s == schedules[49].Id.ToString()),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(
                    new SchedulerConfigPage(
                        null!,
                        schedules.Skip(50).ToList())));

        var list = await m_SchedulerClient.List();

        Assert.That(list.Count, Is.EqualTo(75));
        m_MockApi.VerifyAll();
    }

    [Test]
    public async Task ListWhenThereAreNone()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        m_MockApi.Setup(
                api => api.ListSchedulerConfigsAsync(
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
                    It.Is<int>(i => i == 50),
                    It.Is<string>(s => s == null),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(
                    new SchedulerConfigPage(
                        null!,
                        new List<ScheduleConfig>())));

        var list = await m_SchedulerClient.List();

        Assert.That(list.Count, Is.EqualTo(0));
        m_MockApi.VerifyAll();
    }

    [Test]
    public async Task UpdateMapsToDeleteCreate()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        m_MockApi.Setup(
                api => api.DeleteScheduleConfigAsync(
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
                    m_Schedule.Id,
                    0,
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m_MockApi.Setup(a => a.CreateScheduleConfigAsync(
            It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
            It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
            It.Is<ScheduleConfigBody>(sch =>
                sch.Name == "foo"
                && sch.StartAt == m_Schedule.StartAt
                && sch.EndAt == m_Schedule.EndAt),
            0,
            It.IsAny<CancellationToken>())).Returns(Task.FromResult(new ScheduleConfigId(m_Schedule.Id)));

        await m_SchedulerClient.Update(SchedulerClient.FromResponse(m_Schedule));

        m_MockApi.VerifyAll();
    }

    [Test]
    public async Task UpdateApiExceptionIsWrappedAsClientException()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var apiException = new ApiException();
        m_MockApi.Setup(
                api => api.DeleteScheduleConfigAsync(
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
                    It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
                    m_Schedule.Id,
                    0,
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        var exception = Assert.ThrowsAsync<ClientException>(
            async () => await m_SchedulerClient.Update(SchedulerClient.FromResponse(m_Schedule)));

        Assert.That(exception!.InnerException, Is.SameAs(apiException));
    }

    [Test]
    public async Task CreateMapsToCreate()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        m_MockApi.Setup(a => a.CreateScheduleConfigAsync(
            It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
            It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
            It.Is<ScheduleConfigBody>(sch =>
                sch.Name == "foo"
                && sch.StartAt == m_Schedule.StartAt
                && sch.EndAt == m_Schedule.EndAt),
            0,
            It.IsAny<CancellationToken>())).Returns(Task.FromResult(new ScheduleConfigId(m_Schedule.Id)));

        await m_SchedulerClient.Create(SchedulerClient.FromResponse(m_Schedule));

        m_MockApi.VerifyAll();
    }

    [Test]
    public async Task CreateApiExceptionIsWrappedAsClientException()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var apiException = new ApiException();
        m_MockApi.Setup(a => a.CreateScheduleConfigAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<ScheduleConfigBody>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        var exception = Assert.ThrowsAsync<ClientException>(
            async () => await m_SchedulerClient.Create(SchedulerClient.FromResponse(m_Schedule)));

        Assert.That(exception!.InnerException, Is.SameAs(apiException));
    }

    [Test]
    public async Task DeleteMapsToDelete()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        m_MockApi.Setup(
            api => api.DeleteScheduleConfigAsync(
                It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
                m_Schedule.Id,
                0,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await m_SchedulerClient.Delete(SchedulerClient.FromResponse(m_Schedule));

        m_MockApi.VerifyAll();
    }

    [Test]
    public async Task DeleteApiExceptionIsWrappedAsClientException()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var apiException = new ApiException();
        m_MockApi.Setup(api => api.DeleteScheduleConfigAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        var exception = Assert.ThrowsAsync<ClientException>(
            async () => await m_SchedulerClient.Delete(SchedulerClient.FromResponse(m_Schedule)));

        Assert.That(exception!.InnerException, Is.SameAs(apiException));
    }

    [Test]
    public async Task GetMapsToGet()
    {
        await m_SchedulerClient.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        m_MockApi.Setup(
            api => api.GetScheduleConfigAsync(
                It.Is<Guid>(g => g.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(g => g.ToString() == TestValues.ValidEnvironmentId),
                It.Is<Guid>(g => g == m_Schedule.Id),
                0,
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new ScheduleConfig(
                m_Schedule.Id,
                m_Schedule.Name,
                m_Schedule.EventName,
                m_Schedule.Type,
                m_Schedule.Schedule,
                m_Schedule.PayloadVersion,
                m_Schedule.Payload,
                m_Schedule.StartAt,
                m_Schedule.EndAt)));

        var res = await m_SchedulerClient.Get(m_Schedule.Id.ToString());

        Assert.That(m_Schedule.Id.ToString(), Is.EqualTo(res.Id));
        Assert.That(m_Schedule.Name, Is.EqualTo(res.Name));
        Assert.That(m_Schedule.EventName, Is.EqualTo(res.EventName));
        Assert.That(m_Schedule.Type, Is.EqualTo(res.ScheduleType));
        Assert.That(m_Schedule.Schedule, Is.EqualTo(res.Schedule));
        Assert.That(m_Schedule.PayloadVersion, Is.EqualTo(res.PayloadVersion));
        Assert.That(m_Schedule.Payload, Is.EqualTo(res.Payload));
        Assert.That(m_Schedule.StartAt, Is.EqualTo(res.StartAt));
        Assert.That(m_Schedule.EndAt, Is.EqualTo(res.EndAt));
        m_MockApi.VerifyAll();
    }
}
