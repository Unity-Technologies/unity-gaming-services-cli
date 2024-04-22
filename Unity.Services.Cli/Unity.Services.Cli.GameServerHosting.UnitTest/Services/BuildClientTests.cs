using Moq;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class BuildClientTests
{
    BuildClient? m_Client;
    Mock<IBuildsApiAsync>? m_MockApi;

    [SetUp]
    public void SetUp()
    {
        m_MockApi = new Mock<IBuildsApiAsync>();
        m_Client = new BuildClient(m_MockApi.Object, new GameServerHostingApiConfig());
    }

    [Test]
    public async Task FindByName_WithNoResults_ReturnsNull()
    {
        SetupListResponse(new List<BuildListInner>());

        var res = await m_Client!.FindByName("test");

        Assert.That(res, Is.Null);
    }

    [Test]
    public async Task FindByName_WithOneResult_ReturnsId()
    {
        SetupListResponse(
            new List<BuildListInner>
            {
                new(
                    0,
                    0,
                    "test",
                    buildVersionName: ValidBuildVersionName,
                    ccd: new CCDDetails(),
                    syncStatus: BuildListInner.SyncStatusEnum.SYNCED)
            });

        var res = await m_Client!.FindByName("test");

        Assert.That(res, Is.Not.Null);
    }

    [Test]
    public void FindByName_WithMultipleResults_ThrowsDuplicateException()
    {
        SetupListResponse(
            new List<BuildListInner>
            {
                new(
                    0,
                    0,
                    "test",
                    buildVersionName: ValidBuildVersionName,
                    ccd: new CCDDetails()),
                new(
                    0,
                    0,
                    "test",
                    buildVersionName: ValidBuildVersionName,
                    ccd: new CCDDetails())
            });

        Assert.ThrowsAsync<DuplicateResourceException>(async () => await m_Client!.FindByName("test"));
    }

    [Test]
    public async Task Create_CallsCreateBuildAsync()
    {
        m_MockApi!.Setup(
                a =>
                    a.CreateBuildAsync(
                        Guid.Empty,
                        Guid.Empty,
                        It.IsAny<CreateBuildRequest>(),
                        default,
                        default))
            .ReturnsAsync(
                new CreateBuild200Response(
                    buildName: "test",
                    ccd: new CCDDetails(),
                    buildVersionName: ValidBuildVersionName));

        await m_Client!.Create("test", new MultiplayConfig.BuildDefinition());

        m_MockApi!.Verify(
            a =>
                a.CreateBuildAsync(
                    Guid.Empty,
                    Guid.Empty,
                    It.IsAny<CreateBuildRequest>(),
                    default,
                    default));
    }

    [Test]
    public async Task CreateVersion_CallsCreateNewBuildVersionAsync()
    {
        await m_Client!.CreateVersion(new BuildId(), new CloudBucketId());

        m_MockApi!.Verify(
            a =>
                a.CreateNewBuildVersionAsync(
                    Guid.Empty,
                    Guid.Empty,
                    0,
                    It.IsAny<CreateNewBuildVersionRequest>(),
                    default,
                    default));
    }

    [Test]
    public async Task IsSynced_SucceedsWhenSynced()
    {
        m_MockApi!.Setup(
                c => c.GetBuildAsync(
                    Guid.Empty,
                    Guid.Empty,
                    0,
                    default,
                    default))
            .ReturnsAsync(
                new CreateBuild200Response(
                    buildName: string.Empty,
                    buildVersionName: ValidBuildVersionName,
                    syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCED));

        Assert.That(
            await m_Client!.IsSynced(
                new BuildId
                {
                    Id = 0
                }));
    }

    [Test]
    public async Task IsSynced_FailsWhenNotSynced()
    {
        m_MockApi!.Setup(
                c => c.GetBuildAsync(
                    Guid.Empty,
                    Guid.Empty,
                    0,
                    default,
                    default))
            .ReturnsAsync(
                new CreateBuild200Response(
                    buildName: string.Empty,
                    buildVersionName: ValidBuildVersionName,
                    syncStatus: CreateBuild200Response.SyncStatusEnum.SYNCING));

        Assert.That(
            await m_Client!.IsSynced(
                new BuildId
                {
                    Id = 0
                }),
            Is.Not.True);
    }

    [Test]
    public void IsSynced_ThrowsOnFailure()
    {
        m_MockApi!.Setup(
                c => c.GetBuildAsync(
                    Guid.Empty,
                    Guid.Empty,
                    0,
                    default,
                    default))
            .ReturnsAsync(
                new CreateBuild200Response(
                    buildName: string.Empty,
                    buildVersionName: ValidBuildVersionName,
                    syncStatus: CreateBuild200Response.SyncStatusEnum.FAILED));

        Assert.ThrowsAsync<SyncFailedException>(
            () => m_Client!.IsSynced(
                new BuildId
                {
                    Id = 0
                }));
    }

    void SetupListResponse(List<BuildListInner> results)
    {
        m_MockApi!.Setup(
                a =>
                    a.ListBuildsAsync(
                        Guid.Empty,
                        Guid.Empty,
                        null,
                        null,
                        null,
                        null,
                        null,
                        It.IsAny<string>(),
                        default,
                        default))
            .ReturnsAsync(results);
    }
}
