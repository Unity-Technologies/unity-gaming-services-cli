using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;

namespace Unity.Services.Cli.ServiceAccountAuthentication;

class AuthenticationService : IServiceAccountAuthenticationService
{
    readonly IPersister<string> m_Persister;
    readonly ISystemEnvironmentProvider m_SystemEnvironmentProvider;

    public AuthenticationService(IPersister<string> persister, ISystemEnvironmentProvider environmentProvider)
    {
        m_Persister = persister;
        m_SystemEnvironmentProvider = environmentProvider;
    }

    /// <inheritdoc cref="IServiceAccountAuthenticationService.GetAccessTokenAsync"/>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        string? token = await m_Persister.LoadAsync(cancellationToken) ??
                        AuthenticatorV1.GetTokenFromEnvironmentVariables(m_SystemEnvironmentProvider, out _);

        if (string.IsNullOrEmpty(token))
        {
            throw new MissingAccessTokenException(
                "You are not logged into any service account. Please login using the 'ugs login' command.");
        }

        return token;
    }
}
