using System.Text;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using MultiplayApi = Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.Services;

class ApiClientFactory : MultiplayApi.IBuildsApiFactory, MultiplayApi.IBuildConfigApiFactory, MultiplayApi.IFleetApiFactory, ICloudStorageFactory
{
    readonly IBuildsApiAsync m_BuildsApiAsync;
    readonly IBuildConfigurationsApiAsync m_BuildConfigurationsApiAsync;
    readonly IFleetsApiAsync m_FleetsApiAsync;
    readonly IBucketsApiAsync m_BucketsApiAsync;
    readonly IEntriesApiAsync m_EntriesApiAsync;
    readonly IServiceAccountAuthenticationService m_AuthenticationService;
    readonly GameServerHostingApiConfig m_GameServerHostingApiConfig;

    public ApiClientFactory(
        IBuildsApiAsync buildsApiAsync,
        IBuildConfigurationsApiAsync buildConfigurationsApiAsync,
        IFleetsApiAsync fleetsApiAsync,
        IBucketsApiAsync bucketsApiAsync,
        IEntriesApiAsync entriesApiAsync,
        IServiceAccountAuthenticationService authenticationService,
        GameServerHostingApiConfig multiplayApiConfig)
    {
        m_BuildsApiAsync = buildsApiAsync;
        m_AuthenticationService = authenticationService;
        m_GameServerHostingApiConfig = multiplayApiConfig;
        m_FleetsApiAsync = fleetsApiAsync;
        m_BucketsApiAsync = bucketsApiAsync;
        m_EntriesApiAsync = entriesApiAsync;
        m_BuildConfigurationsApiAsync = buildConfigurationsApiAsync;
    }

    async Task<MultiplayApi.IBuildsApi> MultiplayApi.IBuildsApiFactory.Build()
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync();
        m_BuildsApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        return new BuildClient(m_BuildsApiAsync, m_GameServerHostingApiConfig);
    }

    async Task<MultiplayApi.IBuildConfigApi> MultiplayApi.IBuildConfigApiFactory.Build()
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync();
        m_BuildConfigurationsApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        return new BuildConfigsClient(m_BuildConfigurationsApiAsync, m_GameServerHostingApiConfig);
    }

    async Task<MultiplayApi.IFleetApi> MultiplayApi.IFleetApiFactory.Build()
    {
        var token = await m_AuthenticationService.GetAccessTokenAsync();
        m_FleetsApiAsync.Configuration.DefaultHeaders.SetAccessTokenHeader(token);
        return new FleetClient(m_FleetsApiAsync, m_GameServerHostingApiConfig);
    }

    Task<ICloudStorage> ICloudStorageFactory.Build()
    {
        var client = new HttpClient();
        SetCcdApiKey(m_BucketsApiAsync.Configuration.DefaultHeaders);
        SetCcdApiKey(m_EntriesApiAsync.Configuration.DefaultHeaders);
        return Task.FromResult<ICloudStorage>(new CcdCloudStorageClient(m_BucketsApiAsync, m_EntriesApiAsync, client, m_GameServerHostingApiConfig));
    }

    void SetCcdApiKey(IDictionary<string, string> headers)
    {
        headers["Authorization"] = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($":{m_GameServerHostingApiConfig.CcdApiKey}"))}";
    }
}
