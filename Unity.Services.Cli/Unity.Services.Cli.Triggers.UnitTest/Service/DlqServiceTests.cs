using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Gateway.TriggersApiV1.Generated.Api;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;

namespace Unity.Services.Cli.Triggers.UnitTest.Service;

[TestFixture]
class DlqServiceTests
{
    const string k_TestAccessToken = "test-token";
    const string k_ValidProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    const string k_EventId = "550e8400-e29b-41d4-a716-446655440000";

    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly Mock<IDLQApiAsync> m_DlqApiMock = new();

    DlqService? m_DlqService;

    [SetUp]
    public void SetUp()
    {
        m_ValidatorObject.Reset();
        m_AuthenticationServiceObject.Reset();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_DlqApiMock.Reset();
        m_DlqApiMock.Setup(a => a.Configuration)
            .Returns(new Gateway.TriggersApiV1.Generated.Client.Configuration());

        m_DlqService = new DlqService(
            m_DlqApiMock.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object);
    }

    [Test]
    public async Task ListDlqEventsAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var page = new DLQEventPage(new List<DLQEvent> { new(), new() });
        m_DlqApiMock.Setup(
            t => t.ListDLQEventsAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                0,
                CancellationToken.None)).ReturnsAsync(page);

        var actual = await m_DlqService!.ListDlqEventsAsync(
            k_ValidProjectId, k_ValidEnvironmentId, null, null, null, null, null, null, CancellationToken.None);

        m_DlqApiMock.VerifyAll();
        Assert.AreEqual(2, actual.Count);
    }

    [Test]
    public async Task GetDlqEventAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var dlqEvent = new DLQEvent();
        m_DlqApiMock.Setup(
            t => t.GetDLQEventAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                It.Is<Guid>(id => id.ToString() == k_EventId),
                0,
                CancellationToken.None)).ReturnsAsync(dlqEvent);

        var actual = await m_DlqService!.GetDlqEventAsync(
            k_ValidProjectId, k_ValidEnvironmentId, k_EventId, CancellationToken.None);

        m_DlqApiMock.VerifyAll();
        Assert.IsNotNull(actual);
    }

    [Test]
    public async Task ReplayDlqEventAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DlqApiMock.Setup(
            t => t.ReplayDLQEventAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                It.Is<Guid>(id => id.ToString() == k_EventId),
                0,
                CancellationToken.None));

        await m_DlqService!.ReplayDlqEventAsync(
            k_ValidProjectId, k_ValidEnvironmentId, k_EventId, CancellationToken.None);

        m_DlqApiMock.VerifyAll();
    }

    [Test]
    public async Task DiscardDlqEventAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DlqApiMock.Setup(
            t => t.DiscardDLQEventAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                It.Is<Guid>(id => id.ToString() == k_EventId),
                0,
                CancellationToken.None));

        await m_DlqService!.DiscardDlqEventAsync(
            k_ValidProjectId, k_ValidEnvironmentId, k_EventId, CancellationToken.None);

        m_DlqApiMock.VerifyAll();
    }

    [Test]
    public async Task ReplayAllDlqEventsAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var result = new DLQQueuedResult(5);
        m_DlqApiMock.Setup(
            t => t.ReplayAllDLQEventsAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                0,
                CancellationToken.None)).ReturnsAsync(result);

        var actual = await m_DlqService!.ReplayAllDlqEventsAsync(
            k_ValidProjectId, k_ValidEnvironmentId, CancellationToken.None);

        m_DlqApiMock.VerifyAll();
        Assert.AreEqual(5, actual.QueuedCount);
    }

    [Test]
    public async Task DiscardAllDlqEventsAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var result = new DLQDiscardResult(3);
        m_DlqApiMock.Setup(
            t => t.DiscardAllDLQEventsAsync(
                It.Is<Guid>(id => id.ToString() == k_ValidProjectId),
                It.Is<Guid>(id => id.ToString() == k_ValidEnvironmentId),
                0,
                CancellationToken.None)).ReturnsAsync(result);

        var actual = await m_DlqService!.DiscardAllDlqEventsAsync(
            k_ValidProjectId, k_ValidEnvironmentId, CancellationToken.None);

        m_DlqApiMock.VerifyAll();
        Assert.AreEqual(3, actual.DiscardedCount);
    }
}
