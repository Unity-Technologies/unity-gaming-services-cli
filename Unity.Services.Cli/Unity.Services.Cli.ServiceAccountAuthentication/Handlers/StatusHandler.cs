using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Handlers;

static class StatusHandler
{
    internal const string NoServiceAccountKeysMessage = "No Service Account key stored. Please store your credentials using ugs login or set system"
        + $" environment variables: {AuthenticatorV1.ServiceKeyId} and {AuthenticatorV1.ServiceSecretKey}.";

    internal static async Task GetStatusAsync(
        IAuthenticator authenticator, ISystemEnvironmentProvider environmentProvider, ILogger logger,
        CancellationToken cancellationToken)
    {
        var configToken = await authenticator.GetTokenAsync(cancellationToken);
        var envToken = AuthenticatorV1.GetTokenFromEnvironmentVariables(environmentProvider, out var warning);
        string status;
        var isConfigTokenNull = string.IsNullOrEmpty(configToken);
        var isEnvironmentTokenNull = string.IsNullOrEmpty(envToken);

        if (!isConfigTokenNull && !isEnvironmentTokenNull)
        {
            status = "Using Service Account key from local configuration.";
        }
        else if (!isEnvironmentTokenNull)
        {
            status = "Using Service Account key from system environment variables.";
        }
        else if (!isConfigTokenNull)
        {
            status = "Using Service Account key from local configuration.";
        }
        else
        {
            status = NoServiceAccountKeysMessage;
        }

        logger.LogInformation(status);
        if (!string.IsNullOrEmpty(warning))
        {
            logger.LogWarning(warning);
        }
    }
}
