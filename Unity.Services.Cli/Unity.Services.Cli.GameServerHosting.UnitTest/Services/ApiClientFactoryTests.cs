using Moq;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

public class ApiClientFactoryTests
{
    Dictionary<string, string> m_Headers = new();
    GameServerHostingApiConfig m_ApiConfig = new();
    Mock<IBuildsApiAsync>? m_MockBuildsApi;
    Mock<IBuildConfigurationsApiAsync>? m_MockBuildConfigApi;
    Mock<IFleetsApiAsync>? m_MockFleetsApi;
    Mock<IBucketsApiAsync>? m_MockBucketsApi;
    Mock<IEntriesApiAsync>? m_MockEntriesApi;
    Mock<IServiceAccountAuthenticationService>? m_MockAuthentication;

    [SetUp]
    public void SetUp()
    {
        var gameServerHostingConfig = new Mock<Gateway.GameServerHostingApiV1.Generated.Client.IReadableConfiguration>();
        gameServerHostingConfig.SetupGet(c => c.DefaultHeaders).Returns(m_Headers);

        var ccdConfig = new Mock<Gateway.ContentDeliveryManagementApiV1.Generated.Client.IReadableConfiguration>();
        ccdConfig.SetupGet(c => c.DefaultHeaders).Returns(m_Headers);

        m_MockBuildsApi = new Mock<IBuildsApiAsync>();
        m_MockBuildsApi.SetupGet(a => a.Configuration).Returns(gameServerHostingConfig.Object);

        m_MockBuildConfigApi = new Mock<IBuildConfigurationsApiAsync>();
        m_MockBuildConfigApi.SetupGet(a => a.Configuration).Returns(gameServerHostingConfig.Object);

        m_MockFleetsApi = new Mock<IFleetsApiAsync>();
        m_MockFleetsApi.SetupGet(a => a.Configuration).Returns(gameServerHostingConfig.Object);

        m_MockBucketsApi = new Mock<IBucketsApiAsync>();
        m_MockBucketsApi.SetupGet(a => a.Configuration).Returns(ccdConfig.Object);

        m_MockEntriesApi = new Mock<IEntriesApiAsync>();
        m_MockEntriesApi.SetupGet(a => a.Configuration).Returns(ccdConfig.Object);

        m_MockAuthentication = new Mock<IServiceAccountAuthenticationService>();
    }

    [Test]
    public async Task GameServerHostingApiIBuildsApiFactoryBuild_Authenticates()
    {
        IBuildsApiFactory factory = new ApiClientFactory(
            m_MockBuildsApi!.Object,
            m_MockBuildConfigApi!.Object,
            m_MockFleetsApi!.Object,
            m_MockBucketsApi!.Object,
            m_MockEntriesApi!.Object,
            m_MockAuthentication!.Object,
            m_ApiConfig);

        await factory.Build();

        Assert.That(() => m_Headers.ContainsKey("Authorization"));
    }

    [Test]
    public async Task GameServerHostingApiIBuildConfigurationApiFactoryBuild_Authenticates()
    {
        IBuildConfigApiFactory factory = new ApiClientFactory(
            m_MockBuildsApi!.Object,
            m_MockBuildConfigApi!.Object,
            m_MockFleetsApi!.Object,
            m_MockBucketsApi!.Object,
            m_MockEntriesApi!.Object,
            m_MockAuthentication!.Object,
            m_ApiConfig);

        await factory.Build();

        Assert.That(() => m_Headers.ContainsKey("Authorization"));
    }

    [Test]
    public async Task GameServerHostingApiIFleetsApiFactoryBuild_Authenticates()
    {
        IFleetApiFactory factory = new ApiClientFactory(
            m_MockBuildsApi!.Object,
            m_MockBuildConfigApi!.Object,
            m_MockFleetsApi!.Object,
            m_MockBucketsApi!.Object,
            m_MockEntriesApi!.Object,
            m_MockAuthentication!.Object,
            m_ApiConfig);

        await factory.Build();

        Assert.That(() => m_Headers.ContainsKey("Authorization"));
    }

    [Test]
    public async Task CcdCloudStorageApiFactoryBuild_Authenticates()
    {
        ICloudStorageFactory factory = new ApiClientFactory(
            m_MockBuildsApi!.Object,
            m_MockBuildConfigApi!.Object,
            m_MockFleetsApi!.Object,
            m_MockBucketsApi!.Object,
            m_MockEntriesApi!.Object,
            m_MockAuthentication!.Object,
            m_ApiConfig);

        await factory.Build();

        Assert.That(() => m_Headers.ContainsKey("Authorization"));
    }
}
