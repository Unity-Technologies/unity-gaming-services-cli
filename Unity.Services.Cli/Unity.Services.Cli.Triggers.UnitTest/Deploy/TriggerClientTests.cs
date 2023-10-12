using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Cli.Triggers.UnitTest.Utils;
using Unity.Services.Gateway.TriggersApiV1.Generated.Model;
using TriggerConfig = Unity.Services.Triggers.Authoring.Core.Model.TriggerConfig;

namespace Unity.Services.Cli.Triggers.UnitTest.Deploy;

[TestFixture]
public class TriggerClientTests
{
    const string k_Name = "path1.tr";
    const string k_Path = "foo/path1.tr";
    const string k_Content = "{ id: \"lb1\", name: \"lb_name\", path: \"foo/path1.tr\" }";
    readonly TriggerConfig m_Trigger;

    public TriggerClientTests()
    {
        m_Trigger = new("1","Trigger1", "EventType1", "cloud-code", "actionUrn") { Path = k_Path };
    }

    [Test]
    public void Initialize_Succeed()
    {
        Mock<ITriggersService> service = new();
        var client = new TriggersClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        Assert.AreEqual(client.EnvironmentId, TestValues.ValidEnvironmentId);
        Assert.AreEqual(client.ProjectId, TestValues.ValidProjectId);
        Assert.AreEqual(client.CancellationToken, CancellationToken.None);
    }

    [Test]
    public async Task ListWhenThereAreNone()
    {
        Mock<ITriggersService> service = new();
        var client = new TriggersClient(service.Object, TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);

        service.Setup(
                s => s.GetTriggersAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.FromResult((IEnumerable<Unity.Services.Gateway.TriggersApiV1.Generated.Model.TriggerConfig>)Array.Empty<Unity.Services.Gateway.TriggersApiV1.Generated.Model.TriggerConfig>()));

        var list = await client.List();
        Assert.AreEqual(0, list.Count);
    }

    [Test]
    public async Task UploadMapsToUpload()
    {
        Mock<ITriggersService> service = new();
        var client = new TriggersClient(service.Object, TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        await client.Update(m_Trigger!);

        service
            .Verify(
                s => s.UpdateTriggerAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    m_Trigger.Id,
                    It.Is<TriggerConfigBody>(l => l.Name == m_Trigger.Name),
                    It.IsAny<CancellationToken>()),
                    Times.Once());
    }

    [Test]
    public void UpdateExceptionPropagates()
    {
        Mock<ITriggersService> service = new();
        var client = new TriggersClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        var exceptionMsg = "unknown exception";
        service.Setup(x => x.UpdateTriggerAsync(TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId, "1", It.IsAny<TriggerConfigBody>(), CancellationToken.None))
            .ThrowsAsync(new Exception(exceptionMsg));

        Assert.ThrowsAsync<Exception>( async () => await client.Update(m_Trigger!) );
    }

    [Test]
    public async Task CreateMapsToCreate()
    {
        Mock<ITriggersService> service = new();
        var client = new TriggersClient(service.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        await client.Create(m_Trigger!);

        service
            .Verify(
                s => s.CreateTriggerAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    It.Is<TriggerConfigBody>(l => l.Name == m_Trigger.Name),
                    It.IsAny<CancellationToken>()),
                Times.Once());
    }

    [Test]
    public async Task DeleteMapsToDelete()
    {
        Mock<ITriggersService> serviceMock = new();
        var client = new TriggersClient(serviceMock.Object);
        client.Initialize(TestValues.ValidEnvironmentId, TestValues.ValidProjectId, CancellationToken.None);
        await client.Delete(m_Trigger!);

        serviceMock
            .Verify(
                s => s.DeleteTriggerAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    m_Trigger.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once());
    }
}
