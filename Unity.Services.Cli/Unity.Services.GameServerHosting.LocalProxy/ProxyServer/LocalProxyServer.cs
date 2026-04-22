using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.GameServerHosting.LocalProxy.Model;
using Unity.Services.GameServerHosting.LocalProxy.Service;

namespace Unity.Services.GameServerHosting.LocalProxy.ProxyServer
{
    /// <summary>
    /// Represents a server that listens for incoming HTTP requests, determines whether they are REST or Websocket and handles them appropriately.
    /// REST requests are handled by the local proxy service, and WebSocket requests are upgraded to an open WS connection.
    /// </summary>
    public sealed class LocalProxyServer : IDisposable
    {
        // As specified in the RFC 6455 protocol
        const string k_MagicWsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        readonly ILogger m_Logger;
        readonly IRemoteLocalProxyService m_RemoteLocalProxyService;
        readonly ConcurrentDictionary<string, WebSocket> m_ActiveClients = new();

        bool m_IsStarted;
        bool m_IsShutdown;
        readonly CancellationTokenSource m_CancelSource = new();

        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        public bool IsListening { get; set; }

        public event EventHandler<WebsocketClientConnectedEventArgs> ClientConnected;
        public event EventHandler<WebsocketClientConnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// Creates a new instance of the LocalProxyServer.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="service">The service to make onward HTTP calls to the remove playmode server.</param>
        /// <param name="gameServerHost">The host of the remote playmode server.</param>
        /// <param name="headers">Additional headers to include in HTTP requests.</param>
        public LocalProxyServer(ILogger logger, IRemoteLocalProxyService service, string gameServerHost, Dictionary<string, string> headers = null)
        {
            m_Logger = logger;
            m_RemoteLocalProxyService = service;
            m_RemoteLocalProxyService.GameServerHost = gameServerHost;

            // Set headers on the service if provided
            if (headers != null)
            {
                m_RemoteLocalProxyService.SetHeaders(headers);
            }
        }

        /// <summary>
        /// Starts the LocalProxyServer in a new thread.
        /// </summary>
        /// <param name="address">Network interface the server should listen to.</param>
        /// <param name="port">Port the server should bind to.</param>
        public void Start(IPAddress address, int port)
        {
            if (m_IsStarted || m_CancelSource.IsCancellationRequested)
            {
                throw new InvalidOperationException("Server was already started");
            }

            m_IsStarted = true;
            Task.Run(() => SetupTcpListener(address, port, m_CancelSource.Token), CancellationToken.None);
        }

        /// <summary>
        /// Starts the LocalProxyServer in a new thread, and waits for it to be ready.
        /// </summary>
        /// <param name="address">Network interface the server should listen to.</param>
        /// <param name="port">Port the server should bind to.</param>
        /// <param name="cancellationToken"></param>
        public async Task StartAsync(IPAddress address, int port, CancellationToken cancellationToken = default)
        {
            Start(address, port);

            using var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(m_CancelSource.Token, cancellationToken);
            while (!IsListening && !cancelSource.Token.IsCancellationRequested)
            {
                await Task.Delay(25, cancelSource.Token);
            }
        }

        /// <summary>
        /// Stops the LocalProxy server
        /// </summary>
        public void Stop()
        {
            m_CancelSource.Cancel();
        }

        /// <summary>
        /// Stops the LocalProxy server and waits for the confirmation it is closed properly.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            Stop();

            while (IsListening && !m_IsShutdown && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(25, cancellationToken);
            }
        }

        /// <summary>
        /// Stops and disposes of the LocalProxy server.
        /// </summary>
        public void Dispose()
        {
            Stop();
            m_CancelSource.Dispose();
        }

        /// <summary>
        /// Starts the TcpListener and listens for incoming connections in a blocking fashion.
        /// </summary>
        /// <param name="address">Network interface the server should listen to.</param>
        /// <param name="port">Port the server should bind to.</param>
        /// <param name="cancellationToken">Token used to close the server when it is no longer necessary.</param>
        void SetupTcpListener(IPAddress address, int port, CancellationToken cancellationToken)
        {
            TcpListener tcp = null;
            m_ActiveClients.Clear();

            try
            {
                tcp = new TcpListener(address, port);
                tcp.Start();

                Address = (tcp.LocalEndpoint as IPEndPoint)?.Address;
                Port = (tcp.LocalEndpoint as IPEndPoint)!.Port;

                m_Logger.Info("Server started on {0}:{1}", Address, Port);

                Task.Run(() => PerformRoutineCleanup(cancellationToken), CancellationToken.None);

                ListenForConnections(tcp, cancellationToken);
            }
            finally
            {
                IsListening = false;
                tcp?.Stop();
            }
        }

        /// <summary>
        /// Listen for incoming connections and handle them in a non-blocking fashion. This method is blocking until the
        /// CancellationToken is requesting a cancellation.
        /// </summary>
        /// <param name="server">TcpListener instance.</param>
        /// <param name="cancellationToken">CancellationToken to request a server shutdown.</param>
        void ListenForConnections(TcpListener server, CancellationToken cancellationToken)
        {
            m_Logger.Debug("TCP Server listening for new connections");
            IsListening = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient tcpClient = null;

                try
                {
                    // Using the async version so we can cancel using the CancellationToken as needed.
#pragma warning disable CA2016
                    // We are using the cancellation token on the .Wait() method instead, so we can avoid issue around
                    // ValueTask not guaranteed to block the thread (as surfaced by SonarQube).
                    // ReSharper disable once MethodSupportsCancellation - We are using the CancellationToken on the Wait method instead.
                    var task = server.AcceptTcpClientAsync();
#pragma warning restore CA2016
                    task.Wait(cancellationToken);

                    tcpClient = task.Result;
                }
                catch (OperationCanceledException)
                {
                    // We ignore this error.
                }

                if (tcpClient != null)
                {
                    m_Logger.Debug("New connection established");
                    Task.Run(() => HandleTcpClient(tcpClient, cancellationToken), CancellationToken.None);
                }
            }

            // No need for cancellation token here, since the Shutdown method already defines a maximum time to do so.
            Shutdown(CancellationToken.None).Wait(CancellationToken.None);
        }

        /// <summary>
        ///  Handles the incoming TcpClient. For HTTP REST, this parses the request and determines the appropriate service method to call.
        ///  Otherwise, upgrades it to a WebSocket connection and dispatches an event to notify a new connection is available.
        /// </summary>
        /// <param name="client">Connection to the client, through TCP.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        async Task HandleTcpClient(TcpClient client, CancellationToken cancellationToken)
        {
            var stream = client.GetStream();

            while (client.Available < 3 || cancellationToken.IsCancellationRequested)
            {
                // We wait for the client to send at least 3 bytes before we start reading
                await Task.Delay(25, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();

            var buffer = new byte[client.Available];
            _ = await stream.ReadAsync(buffer, cancellationToken);

            // Translate bytes of request to a string
            var data = Encoding.UTF8.GetString(buffer);
            m_Logger.Debug("Received handshake request: {0}", data);

            // Determine whether the incoming bytes indicate a websocket upgrade request, or a local proxy HTTP request.
            if (data.Contains("Upgrade: websocket") && data.Contains("Sec-WebSocket-Key"))
            {
                m_Logger.Debug("Determined request to be WebSocket!");
                HandleWebSocket(client, stream, data, cancellationToken);
            }
            else
            {
                try
                {
                    m_Logger.Debug("Determined request to be HTTP REST!");
                    await HandleHttpRestRequest(stream, data, cancellationToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error making HTTP request: {e.Message}");
                    await SendInternalServerErrorResponse(stream, e.Message, cancellationToken);
                }
                finally
                {
                    // Important to make sure we close the TCP client once the request has been handled, to free it up.
                    // Only for HTTP requests though, as if it's a websocket request we need to preserve the client,
                    // and closing it terminates the websocket connection early (that was a fun one to find).
                    client.Close();
                }
            }
        }

        /// <summary>
        ///  Handles the incoming request, upgrading it to a WebSocket connection and dispatches an event to notify a new connection is available.
        /// </summary>
        /// <param name="client">Connection to the client, through TCP.</param>
        /// <param name="stream">Bidirectional stream from the TCP socket.</param>
        /// <param name="data">The data already read from the stream, used to determine if the request was HTTP or WS.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        void HandleWebSocket(TcpClient client, NetworkStream stream, string data, CancellationToken cancellationToken)
        {
            // We have 250ms to upgrade the connection to a Websocket. Otherwise, we close it.
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(TimeSpan.FromMilliseconds(250));

            // We link both token, so we can cancel the operation if either of them is cancelled.
            var token = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelSource.Token).Token;

            try
            {
                var ws = PerformHandshake(client, stream, data, token);

                var clientId = client.Client.RemoteEndPoint?.ToString() ?? "UnknownClient";
                var success = m_ActiveClients.TryAdd(clientId, ws);
                if (!success)
                {
                    // This should realistically never happen...
                    m_Logger.Error("Unable to add client to active clients list. Closing connection");
                    ws.Abort();
                    ws.Dispose();
                    return;
                }

                ClientConnected?.Invoke(this, new WebsocketClientConnectedEventArgs(clientId, ws));
            }
            catch (InvalidHandshakeException e)
            {
                m_Logger.Error("Error while upgrading TCP connection to WebSocket: {0}", e.Message);
                var response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 426 Upgrade Required\r\n" +
                    "Content-Type: text/plain\r\n\r\n" +
                    "Upgrade Required");

                stream.Write(response, 0, response.Length);

                stream.Dispose();
                client.Dispose();
            }
            catch (OperationCanceledException)
            {
                stream.Dispose();
                client.Dispose();
            }
            catch (Exception e)
            {
                m_Logger.Error("Error while upgrading TCP connection to WebSocket: {0}", e.Message);

                stream.Dispose();
                client.Dispose();
            }
        }

        /// <summary>
        /// Performs a WebSocket handshake with the client.
        /// </summary>
        /// <param name="client">Connection to the client, through TCP.</param>
        /// <param name="stream">Bidirectional stream from the TCP socket.</param>
        /// <param name="data">The data already read from the stream, used to determine if the request was HTTP or WS.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>A Websocket instance if the handshake is successful. Null otherwise.</returns>
        WebSocket PerformHandshake(TcpClient client, NetworkStream stream, string data, CancellationToken cancellationToken)
        {
            // The upgrade request ALWAYS starts with a GET request
            if (!Regex.IsMatch(data, "^GET", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            {
                throw new InvalidHandshakeException("Receive a non-GET request");
            }

            m_Logger.Debug("Received a connection request from {0}. Starting handshake", client.Client.RemoteEndPoint);

            var secWebsocketKey = Regex.Match(data, "Sec-WebSocket-Key: (.*)", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(secWebsocketKey))
            {
                throw new InvalidHandshakeException("No Sec-WebSocket-Key header found in request");
            }

            var secWebsocketAccept = Convert.ToBase64String(SHA1.HashData(
                Encoding.UTF8.GetBytes(secWebsocketKey + k_MagicWsGuid))
            );

            cancellationToken.ThrowIfCancellationRequested();

            var response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\n" +
                                                  "Connection: Upgrade\r\n" +
                                                  "Upgrade: websocket\r\n" +
                                                  "Sec-WebSocket-Accept: " + secWebsocketAccept + "\r\n\r\n");

            stream.Write(response, 0, response.Length);
            m_Logger.Debug("Handshake successful for client {0}. Upgrading to WebSocket", client.Client.RemoteEndPoint);

            cancellationToken.ThrowIfCancellationRequested();

            var ws = WebSocket.CreateFromStream(stream, true, null, TimeSpan.FromMilliseconds(250));
            return ws;
        }

        /// <summary>
        /// Handles the incoming HTTP REST request, routing it to the relevant service methods to action.
        /// </summary>
        /// <param name="downstream">Bidirectional stream from the downstream (local) TCP socket.</param>
        /// <param name="rawRequest">The raw request data already read from the stream.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>A Websocket instance if the handshake is successful. Null otherwise.</returns>
        async Task HandleHttpRestRequest(NetworkStream downstream, string rawRequest, CancellationToken cancellationToken)
        {
            string method = null, path = null;

            using (var reader = new StringReader(rawRequest))
            {
                var firstLine = await reader.ReadLineAsync(cancellationToken);
                if (firstLine != null)
                {
                    var fields = firstLine.Split(" ");
                    if (fields.Length > 2)
                    {
                        method = fields[0];
                        path = fields[1];
                    }
                }
            }

            if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(path))
            {
                await SendInvalidHttpResponse(downstream, cancellationToken);
                return;
            }

            path = path.TrimEnd('/'); // Remove trailing slash for consistency
            method = method.ToUpperInvariant(); // Normalize method to uppercase

            m_Logger.Debug("Routing to method {0} and path {1}", method, path);

            var routeHandlers = new Dictionary<(string method, string path), Func<Match, Task>>
            {
                { ("GET", "/v4/token"), async _ => await m_RemoteLocalProxyService.HandleFetchUnityJwtToken(downstream, cancellationToken) },
                { ("GET", "/v1/payload/token"), async _ => await m_RemoteLocalProxyService.HandleFetchUnityJwtToken(downstream, cancellationToken) },
                { ("GET", "/v1/payload/allocations/(.*)"), async match => await HandleAllocationPayload(downstream, cancellationToken, match) },
                { ("GET", "/payload/(.*)"), async match => await HandleAllocationPayload(downstream, cancellationToken, match) },
                { ("POST", "/v1/servers/(\\d*)/reservations"), async match => await HandleReserveServer(downstream, match, cancellationToken) },
                { ("DELETE", "/v1/servers/(\\d*)/reservations"), async match => await HandleUnreserveServer(downstream, match, cancellationToken) },
                { ("POST", "/v1/servers/(\\d*)/hold"), async match => await HandleHoldServer(downstream,  rawRequest, match, cancellationToken) },
                { ("GET", "/v1/servers/(\\d*)/hold"), async match => await HandleServerHoldStatus(downstream, match, cancellationToken) },
                { ("DELETE", "/v1/servers/(\\d*)/hold"), async match => await HandleRemoveServerHold(downstream, match, cancellationToken) },
                { ("PATCH", "/v1/servers/(.*)/allocations/(.*)"), async match => await HandleUpdateServerState(downstream, rawRequest, match, cancellationToken) },
                { ("POST", "/v1/server/(.*)/allocation/(.*)/ready-for-players"), async match => await HandleReadyForPlayers(downstream, match, cancellationToken) },
                { ("POST", "/v1/server/(.*)/unready"), async match => await HandleUnreadyForPlayers(downstream, match, cancellationToken) },
            };

            foreach (var route in routeHandlers)
            {
                if (method != route.Key.method)
                {
                    continue;
                }
                var match = Regex.Match(
                    path,
                    route.Key.path,
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(100));

                if (!match.Success) continue;

                await route.Value(match);
                return;
            }

            await SendPathNotFoundResponse(downstream, method, path, cancellationToken);
        }

        async Task HandleUnreadyForPlayers(NetworkStream downstream, Match match, CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();

            await m_RemoteLocalProxyService.HandleUnreadyForPlayers(downstream, serverId, cancellationToken);
        }

        async Task HandleReadyForPlayers(NetworkStream downstream, Match match, CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();
            var allocationId = match.Groups[2].Value.Trim();

            await m_RemoteLocalProxyService.HandleReadyForPlayers(downstream, serverId, allocationId, cancellationToken);
        }

        async Task HandleUpdateServerState(
            NetworkStream downstream,
            string rawRequest,
            Match match,
            CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();
            var allocationId = match.Groups[2].Value.Trim();
            await m_RemoteLocalProxyService.HandleUpdateServerState(
                downstream,
                rawRequest,
                serverId,
                allocationId,
                cancellationToken);
        }

        async Task HandleRemoveServerHold(NetworkStream downstream, Match match, CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();
            await m_RemoteLocalProxyService.HandleRemoveServerHold(downstream, serverId, cancellationToken);
        }

        async Task HandleServerHoldStatus(NetworkStream downstream, Match match, CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();
            await m_RemoteLocalProxyService.HandleServerHoldStatus(downstream, serverId, cancellationToken);
        }

        async Task HandleHoldServer(
            NetworkStream downstream,
            string rawRequest,
            Match match,
            CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();
            await m_RemoteLocalProxyService.HandleHoldServer(downstream, rawRequest, serverId, cancellationToken);
        }

        async Task HandleUnreserveServer(NetworkStream downstream, Match match, CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();
            await m_RemoteLocalProxyService.HandleUnreserveServer(downstream, serverId, cancellationToken);
        }

        async Task HandleReserveServer(NetworkStream downstream, Match match, CancellationToken cancellationToken)
        {
            var serverId = match.Groups[1].Value.Trim();
            await m_RemoteLocalProxyService.HandleReserveServer(downstream, serverId, cancellationToken);
        }

        async Task HandleAllocationPayload(NetworkStream downstream, CancellationToken cancellationToken, Match match)
        {
            var allocationId = match.Groups[1].Value.Trim();
            await m_RemoteLocalProxyService.HandleRetrieveAllocationPayload(downstream, allocationId, cancellationToken);
        }


        /// <summary>
        /// Responds to the downstream connection with an HTTP response indicating the incoming request is targeting
        /// an unsupported method or path in the local proxy API. Response with 404 Not Found.
        /// </summary>
        /// <param name="downstream">Bidirectional stream from the downstream (local) TCP socket.</param>
        /// <param name="method">The method of the incoming HTTP request.</param>
        /// <param name="path">The path of the incoming HTTP request.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        static async Task SendPathNotFoundResponse(Stream downstream, string method, string path, CancellationToken cancellationToken)
        {
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 404 Not Found\r\n" +
                "Content-Type: application/json\r\n\r\n" +
                $"{{\"error\":\"Path {method} {path} not found\"}}");

            await downstream.WriteAsync(response, 0, response.Length, cancellationToken);
        }

        /// <summary>
        /// Responds to the downstream connection with an HTTP response indicating the incoming HTTP request is
        /// invalid and could not be parsed. Response with a 400 Bad Request.
        /// </summary>
        /// <param name="downstream">Bidirectional stream from the downstream (local) TCP socket.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        static async Task SendInvalidHttpResponse(Stream downstream, CancellationToken cancellationToken)
        {
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 400 Bad Request\r\n" +
                "Content-Type: application/json\r\n\r\n" +
                "{\"error\":\"Invalid HTTP Request\"}");

            await downstream.WriteAsync(response, 0, response.Length, cancellationToken);
        }

        /// <summary>
        /// Responds to the downstream connection with an HTTP response indicating something internally has failed.
        /// Responds with a 500 Internal Server Error.
        /// </summary>
        /// <param name="downstream">Bidirectional stream from the downstream (local) TCP socket.</param>
        /// <param name="error">The error message to display with the internal server error.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        static async Task SendInternalServerErrorResponse(Stream downstream, string error, CancellationToken cancellationToken)
        {
            var response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 500 Internal Server Error\r\n" +
                "Content-Type: application/json\r\n\r\n" +
                $"{{\"error\":\"{error}\"}}");

            await downstream.WriteAsync(response, 0, response.Length, cancellationToken);
        }

        /// <summary>
        /// Performs a cleanup of the server, closing all active connections.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        async Task Shutdown(CancellationToken cancellationToken)
        {
            var tasks = m_ActiveClients.Select(async (pair) =>
            {
                if (pair.Value.State != WebSocketState.Open)
                {
                    pair.Value.Abort();
                    pair.Value.Dispose();
                }

                using var cancelToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancelToken.CancelAfter(TimeSpan.FromMilliseconds(250));

                try
                {
                    if (pair.Value.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                    {
                        // We try to be nice...
                        await pair.Value.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server shutting down",
                            cancelToken.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    pair.Value.Abort();
                }

                pair.Value.Dispose();
            });

            await Task.WhenAll(tasks);
            m_IsShutdown = true;
        }

        /// <summary>
        /// Cleans up closed connections from the active clients list.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        void PerformRoutineCleanup(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Task.Delay(100, cancellationToken).Wait(cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var inactiveConnections = m_ActiveClients.Where(pair => pair.Value.State is WebSocketState.Closed or WebSocketState.Aborted);
                    foreach (var pair in inactiveConnections)
                    {
                        ClientDisconnected?.Invoke(this, new WebsocketClientConnectedEventArgs(pair.Key, pair.Value));
                        m_Logger.Debug("Client {0} in state '{1}'. Removing from active clients list", pair.Key, pair.Value.State);
                        m_ActiveClients.TryRemove(pair.Key, out _);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Do nothing
                }
                catch (Exception e)
                {
                    // Handle any exceptions so that in case a failure occurs the routine cleanup still continues.
                    // If we don't do this, then we stop disconnecting from upstream and removing it from disposables,
                    // meaning we can't re-initialise a new connection to upstream when a client re-connects.
                    m_Logger.Debug("Caught exception during PerformRoutineCleanup: {0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }

        /// <summary>
        /// Gets all active connections to clients.
        /// </summary>
        /// <returns>A dictionary of clients, where the key is the RemoteEndpoint, and the value is the Websocket.</returns>
        public IDictionary<string, WebSocket> GetAllConnections()
        {
            return m_ActiveClients;
        }

        /// <summary>
        /// Broadcasts a message across all active connections.
        /// </summary>
        /// <param name="message">Message to broadcast.</param>
        /// <param name="messageType">Type of the message being broadcast.</param>
        /// <param name="endOfMessage">Marks the message as being complete.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public async Task BroadcastMessageAsync(byte[] message, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken = default)
        {
            var tasks = m_ActiveClients.Select(pair =>
            {
                if (pair.Value.State == WebSocketState.Open && messageType != WebSocketMessageType.Close)
                {
                    return pair.Value.SendAsync(message, messageType, endOfMessage, cancellationToken);
                }

                return Task.CompletedTask;
            });

            await Task.WhenAll(tasks);
        }
    }

    public class InvalidHandshakeException : ApplicationException
    {
        public InvalidHandshakeException(string message)
            : base(message)
        {
        }
    }
}
