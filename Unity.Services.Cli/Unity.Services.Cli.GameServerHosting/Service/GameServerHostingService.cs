using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;

namespace Unity.Services.Cli.GameServerHosting.Service;

public class GameServerHostingService : IGameServerHostingService
{
    readonly IServiceAccountAuthenticationService m_AuthenticationService;

    public GameServerHostingService(
        IServiceAccountAuthenticationService authenticationService,
        IBuildsApi buildsApi,
        IBuildConfigurationsApi buildConfigurationsApi,
        IFilesApi filesApi,
        IFleetsApi fleetsApi,
        IMachinesApi machinesApi,
        ICoreDumpApi coreDumpApi,
        IServersApi serversApi
    )
    {
        m_AuthenticationService = authenticationService;
        BuildsApi = buildsApi;
        BuildConfigurationsApi = buildConfigurationsApi;
        FilesApi = filesApi;
        FleetsApi = fleetsApi;
        MachinesApi = machinesApi;
        ServersApi = serversApi;
        CoreDumpApi = coreDumpApi;
    }

    public IBuildsApi BuildsApi { get; }

    public IBuildConfigurationsApi BuildConfigurationsApi { get; }

    public IFilesApi FilesApi { get; }

    public IFleetsApi FleetsApi { get; }

    public IMachinesApi MachinesApi { get; }

    public IServersApi ServersApi { get; }

    public ICoreDumpApi CoreDumpApi { get; }


    public async Task AuthorizeGameServerHostingService(CancellationToken cancellationToken = default)
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync(cancellationToken);
        BuildsApi.Configuration.DefaultHeaders.Add("Authorization", $"Basic {token}");
        BuildConfigurationsApi.Configuration.DefaultHeaders.Add("Authorization", $"Basic {token}");
        FilesApi.Configuration.DefaultHeaders.Add("Authorization", $"Basic {token}");
        FleetsApi.Configuration.DefaultHeaders.Add("Authorization", $"Basic {token}");
        MachinesApi.Configuration.DefaultHeaders.Add("Authorization", $"Basic {token}");
        ServersApi.Configuration.DefaultHeaders.Add("Authorization", $"Basic {token}");
        CoreDumpApi.Configuration.DefaultHeaders.Add("Authorization", $"Basic {token}");
    }
}
