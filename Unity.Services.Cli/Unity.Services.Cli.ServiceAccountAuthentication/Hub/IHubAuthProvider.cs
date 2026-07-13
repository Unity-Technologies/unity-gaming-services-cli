namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub;

interface IHubAuthProvider
{
    Task<LoginResult?> TryLoginAsync(CancellationToken cancellationToken);
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
    bool IsHubAuthToken(string? token);
}
