using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Handlers;

static class StatusHandler
{
    internal const string NoCredentialsMessage =
        "No credentials configured. Run `ugs login`, set the "
        + $"{AuthenticatorV1.ServiceKeyId} + {AuthenticatorV1.ServiceSecretKey} environment "
        + $"variables, or set {AuthenticatorV1.AuthToken} to a pre-minted Bearer JWT.";

    // Kept for backwards compatibility with downstream consumers that
    // referenced the previous status string.
    internal const string NoServiceAccountKeysMessage = NoCredentialsMessage;

    internal const string UsingAuthTokenMessage =
        $"Using Bearer JWT from {AuthenticatorV1.AuthToken} environment variable.";
    internal const string UsingLoginConfigMessage =
        "Using Service Account key from local configuration (saved via `ugs login`).";
    internal const string UsingHubAuthMessage =
        "Using Unity Hub authentication.";
    internal const string UsingServiceKeyEnvMessage =
        $"Using Service Account key from {AuthenticatorV1.ServiceKeyId} and "
        + $"{AuthenticatorV1.ServiceSecretKey} environment variables.";

    internal static async Task GetStatusAsync(
        IAuthenticator authenticator, ISystemEnvironmentProvider environmentProvider,
        IHubAuthProvider hubAuthProvider, ILogger logger,
        CancellationToken cancellationToken)
    {
        // Mirror AuthenticationService.GetAccessTokenAsync precedence:
        //   1. UGS_CLI_AUTH_TOKEN env var (Bearer JWT)
        //   2. Persisted token from `ugs login`
        //   3. UGS_CLI_SERVICE_KEY_ID + UGS_CLI_SERVICE_SECRET_KEY env vars
        var bearerToken = environmentProvider.GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out _);
        var hasBearerToken = !string.IsNullOrWhiteSpace(bearerToken);

        var configToken = await authenticator.GetTokenAsync(cancellationToken);
        var hasConfigToken = !string.IsNullOrEmpty(configToken);
        var isHubAuth = hubAuthProvider.IsHubAuthToken(configToken);
        if (isHubAuth)
            hasConfigToken = false;

        var serviceKeyId = environmentProvider.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceKeyId, out _);
        var serviceSecret = environmentProvider.GetSystemEnvironmentVariable(AuthenticatorV1.ServiceSecretKey, out _);
        var hasServiceKeyEnv = !string.IsNullOrWhiteSpace(serviceKeyId)
            && !string.IsNullOrWhiteSpace(serviceSecret);

        string status;
        if (hasBearerToken)
        {
            status = UsingAuthTokenMessage;
        }
        else if (isHubAuth)
        {
            status = UsingHubAuthMessage;
        }
        else if (hasConfigToken)
        {
            status = UsingLoginConfigMessage;
        }
        else if (hasServiceKeyEnv)
        {
            status = UsingServiceKeyEnvMessage;
        }
        else
        {
            logger.LogInformation(NoCredentialsMessage);
            return;
        }

        logger.LogInformation(status);

        // List any sources that ARE configured but are being overridden by the
        // active one. Helps the user understand why a saved login or env var
        // doesn't appear to be taking effect.
        var overridden = new List<string>();
        if (hasBearerToken)
        {
            if (isHubAuth) overridden.Add("Unity Hub authentication");
            if (hasConfigToken) overridden.Add("saved login (`ugs login`)");
            if (hasServiceKeyEnv) overridden.Add($"{AuthenticatorV1.ServiceKeyId} + {AuthenticatorV1.ServiceSecretKey}");
        }
        else if (isHubAuth)
        {
            if (hasServiceKeyEnv) overridden.Add($"{AuthenticatorV1.ServiceKeyId} + {AuthenticatorV1.ServiceSecretKey}");
        }
        else if (hasConfigToken && hasServiceKeyEnv)
        {
            overridden.Add($"{AuthenticatorV1.ServiceKeyId} + {AuthenticatorV1.ServiceSecretKey}");
        }

        if (overridden.Count > 0)
        {
            logger.LogWarning(
                "The following credential sources are also configured but are being overridden: "
                + string.Join("; ", overridden) + ".");
        }
    }
}
