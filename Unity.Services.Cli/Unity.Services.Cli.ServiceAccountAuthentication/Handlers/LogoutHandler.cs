using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Handlers;

static class LogoutHandler
{
    internal static async Task LogoutAsync(
        IAuthenticator authenticator, ISystemEnvironmentProvider environmentProvider, ILogger logger,
        CancellationToken cancellationToken)
    {
        var response = await authenticator.LogoutAsync(environmentProvider, cancellationToken);

        logger.LogInformation(response.Information);

        if (!string.IsNullOrEmpty(response.Warning))
        {
            logger.LogWarning(response.Warning);
        }
    }
}
