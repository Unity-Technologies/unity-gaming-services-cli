using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.GameServerHosting.LocalProxy.Model;

namespace Unity.Services.GameServerHosting.LocalProxy.ProxyServer
{
    public interface ILocalProxy
    {
        public ProxyState ProxyState { get; }
        public string DisconnectReason { get; }

        public event EventHandler<ProxyStateChangedEventArgs> ProxyStateChanged;
        public event EventHandler<WebsocketMessageReceivedEventArgs> MessageReceived;

        public Task StartAsync(LocalProxyConfig config, CancellationToken cancellationToken = default);
        public void Stop();
        public void Dispose();
    }
}
