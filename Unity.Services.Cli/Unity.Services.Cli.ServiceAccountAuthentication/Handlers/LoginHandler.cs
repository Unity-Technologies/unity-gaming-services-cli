using Microsoft.Extensions.Logging;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Handlers;

static class LoginHandler
{
    public static async Task LoginAsync(
        LoginInput input, IAuthenticator authenticator, ILogger logger, CancellationToken cancellationToken)
    {
        await authenticator.LoginAsync(input, cancellationToken);
        logger.LogInformation("Service Account key saved in local configuration.");
    }
}
