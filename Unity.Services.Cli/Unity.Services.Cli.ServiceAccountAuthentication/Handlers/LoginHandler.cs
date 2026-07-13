using Microsoft.Extensions.Logging;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Handlers;

static class LoginHandler
{
    public static async Task LoginAsync(
        LoginInput input, IAuthenticator authenticator, ILogger logger, CancellationToken cancellationToken)
    {
        var result = await authenticator.LoginAsync(input, cancellationToken);

        if (result.DisplayName is not null)
        {
            logger.LogInformation("Logged in via Unity Hub as {DisplayName}.", result.DisplayName);
            return;
        }

        logger.LogInformation("Service Account key saved in local configuration.");
    }
}
