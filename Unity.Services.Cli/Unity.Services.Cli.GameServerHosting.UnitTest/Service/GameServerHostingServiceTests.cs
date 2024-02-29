using Moq;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;
using Unity.Services.Cli.ServiceAccountAuthentication;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Service;

[TestFixture]
class GameServerHostingServiceTests
{
    [SetUp]
    public void SetUp()
    {
        m_AuthenticationService = new Mock<IServiceAccountAuthenticationService>();
        m_AuthenticationService.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(TestAccessToken));

        m_BuildsApi = new GameServerHostingBuildsApiV1Mock();
        m_BuildsApi.SetUp();

        m_FilesApi = new GameServerHostingFilesApiV1Mock();
        m_FilesApi.SetUp();

        m_FleetsApi = new GameServerHostingFleetsApiV1Mock();
        m_FleetsApi.SetUp();

        m_MachinesApi = new GameServerHostingMachinesApiV1Mock();
        m_MachinesApi.SetUp();

        m_ServersApi = new GameServerHostingServersApiV1Mock();
        m_ServersApi.SetUp();

        m_BuildConfigurationsApi = new GameServerHostingBuildConfigurationsApiV1Mock();
        m_BuildConfigurationsApi.SetUp();

        m_CoreDumpApi = new GameServerHostingCoreDumpApiV1Mock();
        m_CoreDumpApi.SetUp();

        m_GameServerHostingService = new GameServerHostingService(
            m_AuthenticationService.Object,
            m_BuildsApi.DefaultBuildsClient.Object,
            m_BuildConfigurationsApi.DefaultBuildConfigurationsClient.Object,
            m_FilesApi.DefaultFilesClient.Object,
            m_FleetsApi.DefaultFleetsClient.Object,
            m_MachinesApi.DefaultMachinesClient.Object,
            m_CoreDumpApi.DefaultCoreDumpClient.Object,
            m_ServersApi.DefaultServersClient.Object
        );
    }

    Mock<IServiceAccountAuthenticationService>? m_AuthenticationService;
    GameServerHostingBuildsApiV1Mock? m_BuildsApi;
    GameServerHostingFilesApiV1Mock? m_FilesApi;
    GameServerHostingFleetsApiV1Mock? m_FleetsApi;
    GameServerHostingMachinesApiV1Mock? m_MachinesApi;
    GameServerHostingServersApiV1Mock? m_ServersApi;
    GameServerHostingService? m_GameServerHostingService;
    GameServerHostingBuildConfigurationsApiV1Mock? m_BuildConfigurationsApi;
    GameServerHostingCoreDumpApiV1Mock? m_CoreDumpApi;

    [Test]
    public async Task AuthorizeGameServerHostingService()
    {
        await m_GameServerHostingService!.AuthorizeGameServerHostingService(CancellationToken.None);
        m_AuthenticationService!.Verify(a => a.GetAccessTokenAsync(CancellationToken.None), Times.Once);
        Assert.Multiple(
            () =>
            {
                Assert.That(
                    m_BuildsApi!.DefaultBuildsClient.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {TestAccessToken}"));
                Assert.That(
                    m_FilesApi!.DefaultFilesClient.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {TestAccessToken}"));
                Assert.That(
                    m_FleetsApi!.DefaultFleetsClient.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {TestAccessToken}"));
                Assert.That(
                    m_ServersApi!.DefaultServersClient.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {TestAccessToken}"));
                Assert.That(
                    m_BuildConfigurationsApi!.DefaultBuildConfigurationsClient.Object.Configuration.DefaultHeaders[
                        "Authorization"],
                    Is.EqualTo($"Basic {TestAccessToken}"));
                Assert.That(
                    m_MachinesApi!.DefaultMachinesClient.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {TestAccessToken}"));
                Assert.That(
                    m_CoreDumpApi!.DefaultCoreDumpClient.Object.Configuration.DefaultHeaders["Authorization"],
                    Is.EqualTo($"Basic {TestAccessToken}"));
            });
    }
}
