#if FEATURE_HUB_AUTH
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub.TokenExchange;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub;

class HubAuthProvider : IHubAuthProvider
{
    static readonly TimeSpan k_LoginWaitTimeout = TimeSpan.FromMinutes(5);
    readonly IHubIpcClient m_HubIpcClient;
    readonly ITokenExchangeClient m_TokenExchangeClient;

    public HubAuthProvider(IHubIpcClient hubIpcClient, ITokenExchangeClient tokenExchangeClient)
    {
        m_HubIpcClient = hubIpcClient;
        m_TokenExchangeClient = tokenExchangeClient;
    }

    public bool IsHubAuthToken(string? token) => token == AuthenticatorV1.HubAuthMarker;

    public async Task<LoginResult?> TryLoginAsync(CancellationToken cancellationToken)
    {
        if (!await m_HubIpcClient.TryConnectAsync(cancellationToken))
            return null;

        HubUserInfo userInfo;
        if (await m_HubIpcClient.IsLoggedInAsync(cancellationToken))
        {
            userInfo = await m_HubIpcClient.GetUserInfoAsync(cancellationToken);
        }
        else
        {
            userInfo = await m_HubIpcClient.WaitForLoginAsync(k_LoginWaitTimeout, cancellationToken);
        }

        if (!userInfo.Valid || string.IsNullOrEmpty(userInfo.AccessToken))
            return null;

        return new LoginResult(userInfo.DisplayName);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!await m_HubIpcClient.TryConnectAsync(cancellationToken))
        {
            throw new HubIpcUnavailableException(
                "Unity Hub is not running. Start Hub or run `ugs logout` then `ugs login` to switch to service account authentication.");
        }

        var userInfo = await m_HubIpcClient.GetUserInfoAsync(cancellationToken);
        if (!userInfo.Valid || string.IsNullOrEmpty(userInfo.AccessToken))
        {
            throw new HubIpcUnavailableException(
                "Unity Hub user is not signed in. Sign in through Unity Hub or run `ugs logout` then `ugs login` to switch to service account authentication.");
        }

        var unityJwt = await m_TokenExchangeClient.ExchangeAsync(userInfo.AccessToken, cancellationToken);
        return AccessTokenHelper.BearerTokenSchemePrefix + unityJwt;
    }
}
#endif
