namespace Unity.Services.Cli.ServiceAccountAuthentication;

public interface IServiceAccountAuthenticationService
{
    /// <summary>
    /// Get the access token for the current user.
    /// Fails if the user isn't logged in.
    /// </summary>
    /// <returns>
    /// Returns the access token for the current user if they're logged in;
    /// throws otherwise.
    /// </returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
