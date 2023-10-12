using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;
using Unity.Services.Cli.ServiceAccountAuthentication;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

class HandlerCommon
{
    protected static Mock<ILogger>? MockLogger;
    protected static Mock<HttpClient>? MockHttpClient;
    static Mock<IServiceAccountAuthenticationService>? s_AuthenticationServiceObject;
    protected static readonly Mock<IUnityEnvironment> MockUnityEnvironment = new();
    internal static GameServerHostingBuildsApiV1Mock? BuildsApi;
    internal static GameServerHostingFilesApiV1Mock? FilesApi;
    internal static GameServerHostingFleetsApiV1Mock? FleetsApi;
    internal static GameServerHostingServersApiV1Mock? ServersApi;
    internal static GameServerHostingMachinesApiV1Mock? MachinesApi;
    internal static GameServerHostingBuildConfigurationsApiV1Mock? BuildConfigurationsApi;
    protected static GameServerHostingService? GameServerHostingService;

    [SetUp]
    public void SetUp()
    {
        MockLogger = new Mock<ILogger>();
        MockHttpClient = new Mock<HttpClient>();

        s_AuthenticationServiceObject = new Mock<IServiceAccountAuthenticationService>();
        s_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .ReturnsAsync(TestAccessToken);

        BuildsApi = new GameServerHostingBuildsApiV1Mock
        {
            ValidProjects = new List<Guid>
            {
                Guid.Parse(ValidProjectId)
            },
            ValidEnvironments = new List<Guid>
            {
                Guid.Parse(ValidEnvironmentId)
            }
        };
        BuildsApi.SetUp();

        FilesApi = new GameServerHostingFilesApiV1Mock
        {
            ValidProjects = new List<Guid>
            {
                Guid.Parse(ValidProjectId)
            },
            ValidEnvironments = new List<Guid>
            {
                Guid.Parse(ValidEnvironmentId)
            }
        };
        FilesApi.SetUp();

        FleetsApi = new GameServerHostingFleetsApiV1Mock
        {
            ValidProjects = new List<Guid>
            {
                Guid.Parse(ValidProjectId)
            },
            ValidEnvironments = new List<Guid>
            {
                Guid.Parse(ValidEnvironmentId)
            }
        };
        FleetsApi.SetUp();

        MachinesApi = new GameServerHostingMachinesApiV1Mock
        {
            ValidProjects = new List<Guid>
            {
                Guid.Parse(ValidProjectId)
            },
            ValidEnvironments = new List<Guid>
            {
                Guid.Parse(ValidEnvironmentId)
            }
        };
        MachinesApi.SetUp();

        ServersApi = new GameServerHostingServersApiV1Mock
        {
            ValidProjects = new List<Guid>
            {
                Guid.Parse(ValidProjectId)
            },
            ValidEnvironments = new List<Guid>
            {
                Guid.Parse(ValidEnvironmentId)
            }
        };
        ServersApi.SetUp();

        BuildConfigurationsApi = new GameServerHostingBuildConfigurationsApiV1Mock
        {
            ValidProjects = new List<Guid>
            {
                Guid.Parse(ValidProjectId)
            },
            ValidEnvironments = new List<Guid>
            {
                Guid.Parse(ValidEnvironmentId)
            }
        };
        BuildConfigurationsApi.SetUp();

        GameServerHostingService = new GameServerHostingService(
            s_AuthenticationServiceObject.Object,
            BuildsApi.DefaultBuildsClient.Object,
            BuildConfigurationsApi.DefaultBuildConfigurationsClient.Object,
            FilesApi.DefaultFilesClient.Object,
            FleetsApi.DefaultFleetsClient.Object,
            MachinesApi.DefaultMachinesClient.Object,
            ServersApi.DefaultServersClient.Object
        );

        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(ValidEnvironmentId);
    }

    [TearDown]
    public void TearDown()
    {
        // Clear invocations to Mock Environment
        MockUnityEnvironment.Invocations.Clear();
    }
}
