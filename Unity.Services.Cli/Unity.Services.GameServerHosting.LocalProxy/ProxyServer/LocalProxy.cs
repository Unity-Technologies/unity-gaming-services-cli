using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.GameServerHosting.LocalProxy.Model;
using Unity.Services.GameServerHosting.LocalProxy.Service;

namespace Unity.Services.GameServerHosting.LocalProxy.ProxyServer
{
    public class WebsocketClientAuth
    {
        readonly string m_Username;
        readonly string m_Password;

        public WebsocketClientAuth(string username, string password)
        {
            m_Username = username;
            m_Password = password;
        }

        public string ToBasicAuthHeader()
        {
            var authBytes = System.Text.Encoding.UTF8.GetBytes($"{m_Username}:{m_Password}");
            return $"Basic {Convert.ToBase64String(authBytes)}";
        }
    }

    public class LocalProxyConfig
    {
        public string UpstreamHost { get; init; }

        public bool Insecure { get; init; }

        public IPAddress ServerAddress { get; init; } = IPAddress.Loopback;
        public ushort ServerPort { get; init; }
        public WebsocketClientAuth ClientAuth { get; init; }
        public Dictionary<string, string> Headers { get; init; }
    }

    /// <summary>
    /// Local Proxy
    /// </summary>
    public sealed class LocalProxy : IDisposable, ILocalProxy
    {
        const string k_UpstreamSocket = "upstream_socket";
        const string k_WebSocketServer = "websocket_server";
        const string k_UpstreamListener = "upstream_listener";

        readonly ILogger m_Logger;

        /// <summary>
        /// A service that is used just for building LocalProxyServer
        /// </summary>
        readonly IRemoteLocalProxyService m_RemoteLocalProxyService;

        readonly CancellationTokenSource m_ProxyCancelTokenSource = new();
        readonly CancellationTokenSource m_WatchUpstreamConnectionCancelTokenSource = new();

        /// <summary>
        /// Handles initial connection and websocket handshake with cleints.
        /// </summary>
        LocalProxyServer m_LocalProxyServer;

        /// <summary>
        /// The upstream websocket connection.
        /// </summary>
        ClientWebSocket m_UpstreamWebSocket;

        ProxyState m_ProxyState = ProxyState.None;

        /// <summary>
        /// Responsible for tracing resources the need to be disposed if one of the side disconnected.
        /// </summary>
        readonly ConcurrentDictionary<string, IDisposable> m_Disposables = new();

        public IPAddress ServerAddress => m_LocalProxyServer?.Address ?? IPAddress.None;
        public int ServerPort => m_LocalProxyServer?.Port ?? 0;

        LocalProxyConfig m_LocalProxyConfig;

        public ProxyState ProxyState
        {
            get => m_ProxyState;
            private set
            {
                if (m_ProxyState == value) return;

                m_ProxyState = value;
                ProxyStateChanged?.Invoke(this, new ProxyStateChangedEventArgs(value));

                // When the upstream disconnects, we disconnect the whole proxy.
                if (value is ProxyState.Error)
                {
                    DisconnectReason ??= "Upstream server disconnected";
                }
                if (value is ProxyState.Disconnected)
                {
                    DisconnectReason ??= "Either client or upstream disconnected gracefully";
                }
            }
        }

        public string DisconnectReason { get; private set; }

        public event EventHandler<ProxyStateChangedEventArgs> ProxyStateChanged;
        public event EventHandler<WebsocketMessageReceivedEventArgs> MessageReceived;


        public LocalProxy(ILogger logger, IRemoteLocalProxyService service)
        {
            m_Logger = logger;
            m_RemoteLocalProxyService = service;
        }

        /// <summary>
        /// Starts the Local Proxy in a non-blocking way.
        /// </summary>
        /// <param name="config">Proxy configuration</param>
        public void Start(LocalProxyConfig config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.UpstreamHost))
                {
                    throw new ArgumentNullException(nameof(config), "Upstream host must be set");
                }

                m_LocalProxyConfig = config;

                var cancellationToken = m_ProxyCancelTokenSource.Token;

                m_LocalProxyServer = BuildLocalProxyServer();
                SetupWebSocketServer(cancellationToken);

                m_LocalProxyServer.Start(config.ServerAddress, config.ServerPort);
            }
            catch (Exception e)
            {
                ProxyState = ProxyState.Error;
                DisconnectReason = $"Error while setting up the proxy: {e.Message}";

                throw;
            }
        }

        LocalProxyServer BuildLocalProxyServer()
        {
            var server = new LocalProxyServer(m_Logger, m_RemoteLocalProxyService, m_LocalProxyConfig.UpstreamHost, m_LocalProxyConfig.Headers);

            var success = m_Disposables.TryAdd(k_WebSocketServer, server);
            if (!success)
            {
                m_Logger.Error("Failed to add websocket server to disposables");
                server.Dispose();

                throw new InvalidOperationException("Failed to add websocket server to disposables");
            }

            return server;
        }

        /// <summary>
        /// Starts the Local Proxy in a separate thread and waits for it to start.
        /// </summary>
        /// <param name="config">Proxy configuration</param>
        /// <param name="cancellationToken"></param>
        public async Task StartAsync(LocalProxyConfig config, CancellationToken cancellationToken = default)
        {
            Start(config);

            using var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(
                m_ProxyCancelTokenSource.Token,
                cancellationToken);

            while (!m_LocalProxyServer.IsListening && !cancelSource.IsCancellationRequested)
            {
                await Task.Delay(25, cancelSource.Token);
            }

            ProxyState = ProxyState.Awaiting;
        }

        public void Stop()
        {
            DisconnectReason ??= "Proxy stopped";

            if (ProxyState != ProxyState.Disconnected && ProxyState != ProxyState.Error)
            {
                m_ProxyCancelTokenSource.Cancel();
                // We are leaving some time for the upstream socket to close properly and have the state change being reported correctly.
                m_WatchUpstreamConnectionCancelTokenSource.CancelAfter(3000);
            }

            foreach (var garbage in m_Disposables)
            {
                garbage.Value.Dispose();
            }

            m_Disposables.Clear();
        }

        public void Dispose()
        {
            Stop();
            m_ProxyCancelTokenSource.Dispose();

            MessageReceived = null;
            ProxyStateChanged = null;
        }

        /// <summary>
        /// Creates and connects the upstream websocket, along with the connection watch.
        /// </summary>
        /// <param name="cancellationToken"></param>
        ClientWebSocket CreateUpstreamWebSocket(CancellationToken cancellationToken)
        {
            Task.Run(
                () => WatchUpstreamConnection(m_WatchUpstreamConnectionCancelTokenSource.Token),
                CancellationToken.None);

            var upstream = new ClientWebSocket();
            var success = m_Disposables.TryAdd(k_UpstreamSocket, upstream);
            if (!success)
            {
                upstream.Abort();
                upstream.Dispose();
                throw new InvalidOperationException("Failed to add upstream socket to disposables");
            }

            if (m_LocalProxyConfig.ClientAuth != null)
            {
                upstream.Options.SetRequestHeader("Authorization", m_LocalProxyConfig.ClientAuth.ToBasicAuthHeader());
            }

            // Add all headers from LocalProxyConfig
            if (m_LocalProxyConfig.Headers != null)
            {
                foreach (var header in m_LocalProxyConfig.Headers)
                {
                    upstream.Options.SetRequestHeader(header.Key, header.Value);
                }
            }

            upstream.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            var protocol = m_LocalProxyConfig.Insecure ? "ws" : "wss";
            var upstreamWebSocketUri =
                new Uri($"{protocol}://{m_LocalProxyConfig.UpstreamHost}/v1/connection/websocket");

            m_Logger.Debug("Connecting to upstream WS server: {0}", upstreamWebSocketUri);
            upstream.ConnectAsync(upstreamWebSocketUri, cancellationToken)
                .ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted)
                        {
                            m_Logger.Error(
                                "Error connecting to the upstream WS server: {0}",
                                task.Exception?.Message);
                            ProxyState = ProxyState.Error;

                            var exMsg = task.Exception?.InnerExceptions.FirstOrDefault()?.Message ??
                                        task.Exception?.InnerException?.Message ?? "Unknown error";
                            DisconnectReason = $"Unable to connect to upstream server: {upstreamWebSocketUri}: {exMsg}";
                        }
                        else
                        {
                            m_Logger.Debug("Connected to upstream WS server: {0}", upstreamWebSocketUri);
                        }
                    },
                    cancellationToken).GetAwaiter().GetResult();

            return upstream;
        }

        void SetupUpstreamListener(CancellationToken cancellationToken = default)
        {
            var upstreamListener = new WebsocketListener(
                "upstream",
                m_UpstreamWebSocket,
                WebSocketSource.Upstream,
                m_Logger);

            // Add to the list of composed elements
            var success = m_Disposables.TryAdd(k_UpstreamListener, upstreamListener);
            if (!success)
            {
                m_Logger.Error("Failed to add upstream listener to disposables");
                upstreamListener.Dispose();

                throw new InvalidOperationException("Failed to add upstream listener to disposables");
            }

            upstreamListener.MessageReceived += BuildUpstreamMessageReceivedHandler(cancellationToken);

            // Forwarding the message onto whoever is interested.
            upstreamListener.MessageReceived += (sender, e) => MessageReceived?.Invoke(sender, e);

            upstreamListener.Start();
        }

        void SetupWebSocketServer(CancellationToken cancellationToken = default)
        {
            m_LocalProxyServer.ClientConnected += BuildClientConnectedHandler(cancellationToken);
            m_LocalProxyServer.ClientDisconnected += BuildClientDisconnectedHandler(cancellationToken);
        }

        /// <summary>
        /// Stops the upstream connection and cleans up the resources.
        /// </summary>
        /// <param name="cancellationToken"></param>
        void StopUpstreamConnection(CancellationToken cancellationToken)
        {
            // Dealing with the upstream connection
            if (m_UpstreamWebSocket != null)
            {
                m_UpstreamWebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Proxy stopped",
                        CancellationToken.None)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            m_Logger.Error("Error closing upstream socket: {0}", t.Exception?.Message);
                        }
                    },
                    cancellationToken)
                    .GetAwaiter().GetResult();

                m_Disposables.TryRemove(k_UpstreamSocket, out _);

                m_UpstreamWebSocket.Dispose();
                m_UpstreamWebSocket = null;
            }

            // Dealing with the upstream listener
            if (m_Disposables.TryRemove(k_UpstreamListener, out var upstreamWebSocketListener))
            {
                upstreamWebSocketListener.Dispose();
            }
            else
            {
                m_Logger.Debug("Can't remove upstream listener from disposables. Probably it wasn't created yet and it's an initial connection");
            }
        }

        EventHandler<WebsocketClientConnectedEventArgs> BuildClientConnectedHandler(CancellationToken cancellationToken)
        {
            return (_, eventArgs) =>
            {
                m_Logger.Debug("Client connected: {0}", eventArgs.ClientId);

                m_UpstreamWebSocket = CreateUpstreamWebSocket(cancellationToken);
                SetupUpstreamListener(cancellationToken);

                // When a client connects, we need to set up a listener for it and proxy the messages to the upstream.
                // We also ensure we add the listener to the list of disposables to clean up when the proxy is stopped.
                // The WebSocketServer is already in charge of cleaning up after the sockets themselves.
                var clientListener = new WebsocketListener(
                    eventArgs.ClientId,
                    eventArgs.WebSocket,
                    WebSocketSource.Client,
                    m_Logger);

                if (!m_Disposables.TryAdd(eventArgs.ClientId, clientListener))
                {
                    m_Logger.Error("Failed to add listener to disposables. Closing listener.");
                    clientListener.Dispose();

                    // We attempt to close the connection with an internal server error. We then close the socket properly.
                    if (eventArgs.WebSocket.State == WebSocketState.Open)
                    {
                        eventArgs.WebSocket.CloseAsync(
                                WebSocketCloseStatus.InternalServerError,
                                "Internal error",
                                cancellationToken)
                            .ContinueWith(
                                t =>
                                {
                                    if (t.IsFaulted)
                                    {
                                        eventArgs.WebSocket.Abort();
                                    }

                                    eventArgs.WebSocket.Dispose();
                                },
                                cancellationToken);
                    }
                    else
                    {
                        // If the socket is in a weird state, we abort and dispose of it.
                        eventArgs.WebSocket.Abort();
                        eventArgs.WebSocket.Dispose();
                    }
                }

                clientListener.MessageReceived += BuildClientMessageReceivedHandler(cancellationToken);

                // Forwarding the message onto whoever is interested.
                clientListener.MessageReceived += (sender, e) => MessageReceived?.Invoke(sender, e);


                clientListener.Start();
            };
        }

        EventHandler<WebsocketClientConnectedEventArgs> BuildClientDisconnectedHandler(
            CancellationToken cancellationToken)
        {
            return (_, clientId) =>
            {
                m_Logger.Debug("Client disconnected: {0}", clientId.ClientId);
                m_Logger.Debug("Removing the client from the websocket listeners and stopping the upstream connection");
                StopUpstreamConnection(cancellationToken);
                if (m_Disposables.TryRemove(clientId.ClientId, out var clientWebSocketConnection))
                {
                    clientWebSocketConnection.Dispose();
                }
                else
                {
                    m_Logger.Debug("Client doesn't not exist in disposables");
                }

                // Switch the proxy state that it awaits for a new client to connect.
                ProxyState = ProxyState.Awaiting;
            };
        }

        EventHandler<WebsocketMessageReceivedEventArgs> BuildUpstreamMessageReceivedHandler(
            CancellationToken cancellationToken)
        {
            return (_, msgArgs) =>
            {
                m_LocalProxyServer.BroadcastMessageAsync(
                        msgArgs.Message,
                        msgArgs.MessageType,
                        true,
                        cancellationToken)
                    .GetAwaiter();
            };
        }
        EventHandler<WebsocketMessageReceivedEventArgs> BuildClientMessageReceivedHandler(
            CancellationToken cancellationToken)
        {
            return (_, msgArgs) =>
            {
                if (msgArgs.MessageType == WebSocketMessageType.Close)
                {
                    m_UpstreamWebSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            string.Empty,
                            cancellationToken)
                        .ContinueWith(
                            t =>
                            {
                                if (t.IsFaulted)
                                {
                                    m_Logger.Error("Error closing upstream socket: {0}", t.Exception?.Message);
                                }
                            },
                            cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    // Fire and forget mode
                    m_UpstreamWebSocket.SendAsync(
                            msgArgs.Message,
                            msgArgs.MessageType,
                            true,
                            cancellationToken);
                }
            };
        }

        /// <summary>
        /// Watches the upstream connection every so often to change the LocalProxy state accordingly.
        /// </summary>
        /// <param name="cancellationToken"></param>
        void WatchUpstreamConnection(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Task.Delay(100, cancellationToken).Wait(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (m_UpstreamWebSocket == null)
                {
                    continue;
                }

                if (ProxyState is ProxyState.Disconnected or ProxyState.Error) return;

                switch (m_UpstreamWebSocket.State)
                {
                    case WebSocketState.None:
                    case WebSocketState.Connecting:
                        ProxyState = ProxyState.Connecting;
                        break;
                    case WebSocketState.Open:
                        ProxyState = ProxyState.Connected;
                        break;
                    case WebSocketState.CloseSent:
                    case WebSocketState.CloseReceived:
                    case WebSocketState.Closed:
                    case WebSocketState.Aborted:
                        ProxyState = ProxyState.Disconnected;
                        return;
                    default:
                        m_Logger.Warn("Unknown WebSocket state: {0}", m_UpstreamWebSocket.State);
                        break;
                }
            }
        }
    }
}
