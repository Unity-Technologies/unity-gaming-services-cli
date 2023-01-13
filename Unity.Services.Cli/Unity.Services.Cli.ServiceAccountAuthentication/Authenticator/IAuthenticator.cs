using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;

namespace Unity.Services.Cli.ServiceAccountAuthentication;

interface IAuthenticator
{
    Task LoginAsync(LoginInput input, CancellationToken cancellationToken = default);

    Task<LogoutResponse> LogoutAsync(ISystemEnvironmentProvider environmentProvider,
        CancellationToken cancellationToken = default);

    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}
