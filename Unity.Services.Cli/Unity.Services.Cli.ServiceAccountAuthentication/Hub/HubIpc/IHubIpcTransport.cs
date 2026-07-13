#if FEATURE_HUB_AUTH
namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

interface IHubIpcTransport : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Stream GetStream();
}
#endif
