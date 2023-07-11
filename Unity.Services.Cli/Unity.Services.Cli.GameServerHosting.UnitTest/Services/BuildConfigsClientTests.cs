using Moq;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class BuildConfigsClientTests
{
    Mock<IBuildConfigurationsApiAsync>? m_MockApi;
    BuildConfigsClient? m_Client;

    [SetUp]
    public void SetUp()
    {
        m_MockApi = new Mock<IBuildConfigurationsApiAsync>();
        m_Client = new BuildConfigsClient(m_MockApi.Object, new GameServerHostingApiConfig());
    }

    [Test]
    public async Task FindByName_WithNoResults_ReturnsNull()
    {
        m_MockApi!.Setup(a =>
                a.ListBuildConfigurationsAsync(
                    Guid.Empty,
                    Guid.Empty,
                    null,
                    "test",
                    default,
                    default))
            .ReturnsAsync(new List<BuildConfigurationListItem>());

        var res = await m_Client!.FindByName("test");

        Assert.That(res, Is.Null);
    }

    [Test]
    public async Task FindByName_WithOneResult_ReturnsId()
    {
        m_MockApi!.Setup(a =>
                a.ListBuildConfigurationsAsync(
                    Guid.Empty,
                    Guid.Empty,
                    null,
                    "test",
                    default,
                    default))
            .ReturnsAsync(new List<BuildConfigurationListItem>
            {
                new (0, string.Empty, DateTime.Now, updatedAt: DateTime.Now, name: "test")
            });

        var res = await m_Client!.FindByName("test");

        Assert.That(res, Is.Not.Null);
    }

    [Test]
    public void FindByName_WithMultipleResults_ThrowsDuplicateException()
    {
        m_MockApi!.Setup(a =>
                a.ListBuildConfigurationsAsync(
                    Guid.Empty,
                    Guid.Empty,
                    null,
                    "test",
                    default,
                    default))
            .ReturnsAsync(new List<BuildConfigurationListItem>
            {
                new (0, string.Empty, DateTime.Now, updatedAt: DateTime.Now, name: "test"),
                new (0, string.Empty, DateTime.Now, updatedAt: DateTime.Now, name: "test")
            });

        Assert.ThrowsAsync<DuplicateResourceException>(async () => await m_Client!.FindByName("test"));
    }

    [Test]
    public async Task Create_CallsCreateApi()
    {
        m_MockApi!.Setup(a =>
            a.CreateBuildConfigurationAsync(
                Guid.Empty,
                Guid.Empty,
                It.IsAny<BuildConfigurationCreateRequest>(),
                default,
                default))
            .ReturnsAsync(new BuildConfiguration(
                string.Empty,
                0,
                string.Empty,
                string.Empty,
                new List<ConfigEntry>(),
                name: "test",
                queryType: "sqp"));

        await m_Client!.Create("test", new BuildId(), new MultiplayConfig.BuildConfigurationDefinition
        {
            BinaryPath = string.Empty,
            CommandLine = string.Empty
        });

        m_MockApi!.Verify(a =>
            a.CreateBuildConfigurationAsync(
                Guid.Empty,
                Guid.Empty,
                It.IsAny<BuildConfigurationCreateRequest>(),
                default,
                default));
    }

    [Test]
    public async Task Update_CallsUpdateApi()
    {
        await m_Client!.Update(new BuildConfigurationId(), "test", new BuildId(), new MultiplayConfig.BuildConfigurationDefinition
        {
            BinaryPath = string.Empty,
            CommandLine = string.Empty
        });

        m_MockApi!.Verify(a =>
            a.UpdateBuildConfigurationAsync(
                Guid.Empty,
                Guid.Empty,
                0,
                It.IsAny<BuildConfigurationUpdateRequest>(),
                default,
                default));
    }
}
