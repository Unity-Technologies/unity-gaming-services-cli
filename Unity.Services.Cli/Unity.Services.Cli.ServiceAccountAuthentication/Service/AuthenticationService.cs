using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;

namespace Unity.Services.Cli.ServiceAccountAuthentication;

class AuthenticationService : IServiceAccountAuthenticationService
{
    readonly IPersister<string> m_Persister;
    readonly ISystemEnvironmentProvider m_SystemEnvironmentProvider;
    readonly IHubAuthProvider m_HubAuthProvider;

    public AuthenticationService(
        IPersister<string> persister,
        ISystemEnvironmentProvider environmentProvider,
        IHubAuthProvider hubAuthProvider)
    {
        m_Persister = persister;
        m_SystemEnvironmentProvider = environmentProvider;
        m_HubAuthProvider = hubAuthProvider;
    }

    /// <inheritdoc cref="IServiceAccountAuthenticationService.GetAccessTokenAsync"/>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Precedence:
        //   1. UGS_CLI_AUTH_TOKEN or UGS_CLI_USE_HUB_AUTH — explicit per-invocation Bearer JWT.
        //      Beats the persisted login because callers that set it (e.g.
        //      orchestration tools embedding the CLI) want *this* credential
        //      for *this* call regardless of any stale `ugs login` state on
        //      disk.
        //   2. Persisted token from `ugs login` — long-lived service-account
        //      basic auth for interactive use.
        //   3. UGS_CLI_SERVICE_KEY_ID + UGS_CLI_SERVICE_SECRET_KEY env vars
        //      — service-account basic auth supplied via the environment.
        //      — hub auth force enabled
        string? bearer = m_SystemEnvironmentProvider
            .GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out _);
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            return AccessTokenHelper.BearerTokenSchemePrefix + bearer;
        }

        var hubToken = await CheckForEnvHubToken(cancellationToken);
        if (!string.IsNullOrWhiteSpace(hubToken))
        {
            return hubToken;
        }

        string? token = await m_Persister.LoadAsync(cancellationToken);
        token ??= AuthenticatorV1.GetTokenFromEnvironmentVariables(m_SystemEnvironmentProvider, out _);

        if (m_HubAuthProvider.IsHubAuthToken(token))
        {
            return await m_HubAuthProvider.GetAccessTokenAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(token))
        {
            throw new MissingAccessTokenException(
                "You are not logged into any service account. Please login using the 'ugs login' command.");
        }

        return token;
    }

    async Task<string?> CheckForEnvHubToken(CancellationToken cancellationToken)
    {
        var useHubAuthRaw = m_SystemEnvironmentProvider
            .GetSystemEnvironmentVariable(AuthenticatorV1.HubAuthEnv, out _);
        return useHubAuthRaw?.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "y" or "t" => await m_HubAuthProvider.GetAccessTokenAsync(cancellationToken),
            _ => null
        };
    }
}
