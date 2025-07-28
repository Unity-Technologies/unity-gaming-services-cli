using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.GameServerHosting.LocalProxy.Model;

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.Model;

class TestWebSocketServer(IPAddress address, ILogger logger) : IDisposable
{
    public readonly IPAddress Address = address;
    public int Port;

    HttpListener m_HttpListener;

    public void StartAsync(CancellationToken cancellationToken)
    {
        m_HttpListener = new HttpListener();

        TryToStartOnRandomPort(Address, out m_HttpListener, out Port);

        Task.Run(
                async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var context = await m_HttpListener.GetContextAsync();
                        if (context.Request.IsWebSocketRequest)
                        {
                            var webSocketContext = await context.AcceptWebSocketAsync(null);
                            _ = HandleWebSocketAsync(webSocketContext.WebSocket, cancellationToken);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }

                    logger.Debug("Stopping WebSocket server due to cancellation");
                    Stop();
                },
                cancellationToken)
            .GetAwaiter();
    }

    public void Stop()
    {
        if (m_HttpListener is not { IsListening: true }) return;

        m_HttpListener.Stop();
        m_HttpListener.Close();
        logger.Debug("WebSocket server stopped");
    }

    async Task HandleWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        logger.Debug("Client connected");
        var buffer = new byte[1024];

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.Debug("Client disconnected");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    logger.Debug($"Received: {message}");

                    var response = Encoding.UTF8.GetBytes($"Echo: {message}");
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(response),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error: {ex.Message}");
        }
        finally
        {
            webSocket.Dispose();
        }
    }

    static void TryToStartOnRandomPort(IPAddress address, out HttpListener httpListener, out int port)
    {
        // IANA suggested range for dynamic or private ports
        const int minPort = 49215;
        const int maxPort = 65535;

        for (port = minPort; port < maxPort; port++)
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://{address}:{port}/");
            try
            {
                httpListener.Start();
                return;
            }
            catch
            {
                // nothing to do here -- the listener disposes itself when Start throws
            }
        }

        throw new InvalidOperationException("No free ports available");
    }

    public void Dispose()
    {
        logger.Debug("Stopping WebSocket server due to disposal");
        Stop();
    }
}
