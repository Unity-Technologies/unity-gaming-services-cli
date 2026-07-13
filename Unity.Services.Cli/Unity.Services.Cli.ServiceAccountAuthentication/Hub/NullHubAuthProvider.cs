namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub;

class NullHubAuthProvider : IHubAuthProvider
{
    public Task<LoginResult?> TryLoginAsync(CancellationToken cancellationToken)
        => Task.FromResult<LoginResult?>(null);

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        => throw new InvalidOperationException("Hub authentication is not available.");

    public bool IsHubAuthToken(string? token) => false;
}
