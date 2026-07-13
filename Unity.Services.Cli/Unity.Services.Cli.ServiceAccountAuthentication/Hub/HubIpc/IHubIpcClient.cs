#if FEATURE_HUB_AUTH
namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

interface IHubIpcClient : IAsyncDisposable
{
    Task<bool> TryConnectAsync(CancellationToken cancellationToken);
    Task<bool> IsLoggedInAsync(CancellationToken cancellationToken);
    Task<HubUserInfo> GetUserInfoAsync(CancellationToken cancellationToken);
    Task<HubUserInfo> WaitForLoginAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
#endif
