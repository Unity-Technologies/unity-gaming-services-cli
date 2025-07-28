using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.Services.GameServerHosting.LocalProxy.Model;
using Unity.Services.GameServerHosting.LocalProxy.ProxyServer;
using Unity.Services.GameServerHosting.LocalProxy.Service;
using Unity.Services.GameServerHosting.LocalProxy.UnitTest.Model;
using HttpMethod = System.Net.Http.HttpMethod;

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.ProxyServer
{
    public class LocalProxyServerTests
    {
        ILogger m_Logger;
        ILogger m_TestLogger;

        const string k_ValidAllocationId = "00000000-0000-0000-0000-000000000030";
        const string k_ValidServerId = "1234";

        [SetUp]
        public void SetUp()
        {
            m_Logger = new NilLogger();
            m_TestLogger = new TestLogger();
        }

        [Test]
        public async Task Start_ShouldAcceptValidWebsocketClients()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();

            var server = new LocalProxyServer(m_Logger, mockService.Object, "TODO");
            var client = new ClientWebSocket();

            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(10000);

            try
            {
                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);
                await client.ConnectAsync(new Uri($"ws://{server.Address}:{server.Port}"), cancelSource.Token);

                while (client.State != WebSocketState.Open && !cancelSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(25, CancellationToken.None);
                }

                Assert.Multiple(() =>
                {
                    Assert.That(client.State, Is.EqualTo(WebSocketState.Open));
                    Assert.That(server.GetAllConnections(), Has.Count.EqualTo(1));
                });
            }
            finally
            {
                client.Abort();
                client.Dispose();
                server.Dispose();
            }
        }

        [Test]
        public void Start_ShouldThrowIfAlreadyDisposed()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();
            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");

            try
            {
                server.Stop();
                Assert.Throws<InvalidOperationException>(() => server.Start(IPAddress.Loopback, 0), "Server should not be started after being disposed");
            }
            finally
            {
                server.Dispose();
            }
        }

        [Test]
        public void Start_ShouldThrowIfAlreadyStarted()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();
            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");

            try
            {
                server.Start(IPAddress.Loopback, 0);
                Assert.Throws<InvalidOperationException>(() => server.Start(IPAddress.Loopback, 0), "Server should not be started twice");
            }
            finally
            {
                server.Dispose();
            }
        }

        [Test]
        public async Task Start_ShouldHandleHttpRequest()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();

            const string expectedContent = """{"token": "1234", "error": null}""";

            mockService.Reset();
            mockService
                .Setup(x => x.HandleFetchUnityJwtToken(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback(
                    async (Stream stream, CancellationToken token) =>
                {
                    var resp = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 200 OK\r\n" +
                        $"Content-Length: {expectedContent.Length}\r\n" +
                        "Content-Type: application/json\r\n\r\n" +
                        expectedContent
                    );
                    await stream.WriteAsync(
                        resp,
                        0,
                        resp.Length,
                        token);
                })
                .Returns(Task.CompletedTask);

            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");
            var client = new HttpClient();

            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(10000);

            try
            {
                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                var response = await client.GetAsync($"http://{server.Address}:{server.Port}/v1/payload/token", cancellationToken: cancelSource.Token);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var responseStr = await response.Content.ReadAsStringAsync(cancelSource.Token);
                Assert.That(responseStr, Is.EqualTo(expectedContent));

                mockService.Verify(ex => ex
                    .HandleFetchUnityJwtToken(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                server.Dispose();
                client.Dispose();
            }
        }

        [TestCase("GET", "v1/payload/token", "HandleFetchUnityJwtToken")]
        [TestCase("GET", $"v1/payload/allocations/{k_ValidAllocationId}", $"HandleRetrieveAllocationPayload(NetworkStream, \"{k_ValidAllocationId}\", CancellationToken)")]
        [TestCase("PATCH", $"v1/servers/{k_ValidServerId}/allocations/{k_ValidAllocationId}", "HandleUpdateServerState")]
        [TestCase("POST", $"v1/servers/{k_ValidServerId}/reservations", $"HandleReserveServer(NetworkStream, \"{k_ValidServerId}\", CancellationToken)")]
        [TestCase("DELETE", $"v1/servers/{k_ValidServerId}/reservations", $"HandleUnreserveServer(NetworkStream, \"{k_ValidServerId}\", CancellationToken)")]
        [TestCase("POST", $"v1/servers/{k_ValidServerId}/hold", "HandleHoldServer")]
        [TestCase("GET", $"v1/servers/{k_ValidServerId}/hold", $"HandleServerHoldStatus(NetworkStream, \"{k_ValidServerId}\", CancellationToken)")]
        [TestCase("POST", $"v1/server/{k_ValidServerId}/allocation/{k_ValidAllocationId}/ready-for-players", $"HandleReadyForPlayers(NetworkStream, \"{k_ValidServerId}\", \"{k_ValidAllocationId}\", CancellationToken)")]
        [TestCase("POST", $"v1/server/{k_ValidServerId}/unready", $"HandleUnreadyForPlayers(NetworkStream, \"{k_ValidServerId}\", CancellationToken)")]
        [TestCase("DELETE", $"v1/servers/{k_ValidServerId}/hold", $"HandleRemoveServerHold(NetworkStream, \"{k_ValidServerId}\", CancellationToken)")]
        [NonParallelizable]
        public async Task Start_ShouldRouteHTTPRequestsCorrectly(string method, string path, string expectedInvocation)
        {
            var mockService = new Mock<IRemoteLocalProxyService>();
            mockService.Reset();

            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");
            var client = new HttpClient();

            try
            {
                // Very short, as we expect cancellation. But not so short it never gets to the handler in question.
                using var cancelSource = new CancellationTokenSource();
                cancelSource.CancelAfter(250);

                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                var httpMethod = new HttpMethod(method);
                await client.SendAsync(
                    new HttpRequestMessage(httpMethod, $"http://{server.Address}:{server.Port}/{path}"),
                    cancelSource.Token);
            }
            catch (Exception e)
            {
                // Instead of setting up mocks for each of the methods, just let it cancel or fail and catch it here.
                // Then we check if the expected invocation is amongst those being made to the mock service.
                // Note that some have more detailed checks to ensure the parameters we're passing in are accurate,
                // while others pass in the rawRequest so we just check they're being invoked.
                m_TestLogger.Info("Got exception: {0}: {1}", e.Message, e.InnerException);
            }
            finally
            {
                var hasBeenInvoked = mockService.Invocations.Any(
                    mockServiceInvocation => mockServiceInvocation.ToString()!.Contains(expectedInvocation));

                if (!hasBeenInvoked)
                {

                    m_TestLogger.Info("Got invocations:\n[{0}]", string.Join("\n", mockService.Invocations));
                }

                Assert.That(hasBeenInvoked, Is.True);

                server.Dispose();
                client.Dispose();
            }
        }

        [TestCase("GET", "invalid/path")]
        [TestCase("POST", "/v4/token")]
        [TestCase("PUT", "v1/servers/123/reservations")]
        [TestCase("PUT", "v1/servers/123/hold")]
        [TestCase("PUT", "v1/servers/123/allocations/123")]
        [NonParallelizable]
        public async Task Start_ShouldHandleInvalidRoutes(string method, string path)
        {
            var mockService = new Mock<IRemoteLocalProxyService>();
            mockService.Reset();

            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");
            var client = new HttpClient();

            HttpResponseMessage response;
            try
            {
                using var cancelSource = new CancellationTokenSource();
                cancelSource.CancelAfter(1000);

                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                var httpMethod = new HttpMethod(method);
                response = await client.SendAsync(
                    new HttpRequestMessage(httpMethod, $"http://{server.Address}:{server.Port}/{path}"),
                    cancelSource.Token);
            }
            finally
            {
                server.Dispose();
                client.Dispose();
            }

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task Start_ShouldHandleBadHttpRequest()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();
            mockService.Reset();

            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");
            var tcpClient = new TcpClient();

            try
            {
                using var cancelSource = new CancellationTokenSource();
                cancelSource.CancelAfter(1000);

                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                await tcpClient.ConnectAsync(server.Address, server.Port, cancelSource.Token);

                var msg = "HTTP/1.1 INVALID"u8.ToArray();

                var stream = tcpClient.GetStream();
                await stream.WriteAsync(
                    msg,
                    0,
                    msg.Length,
                    cancelSource.Token);

                while (tcpClient.Available < 3 || cancelSource.Token.IsCancellationRequested)
                {
                    // We wait for the client to send at least 3 bytes before we start reading
                    await Task.Delay(25, cancelSource.Token);
                }

                var buffer = new byte[tcpClient.Available];
                _ = await stream.ReadAsync(buffer, 0, buffer.Length, cancelSource.Token);
                StringAssert.Contains("HTTP/1.1 400 Bad Request", Encoding.UTF8.GetString(buffer));
            }
            finally
            {
                server.Dispose();
                tcpClient.Dispose();
            }
        }

        [Test]
        public async Task Start_ThrownExceptionShouldReturnInternalServerError()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();

            mockService.Reset();
            mockService
                .Setup(x => x.HandleFetchUnityJwtToken(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Throws(new HttpRequestException("intentional fail for testing"));

            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");
            var client = new HttpClient();

            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(10000);

            try
            {
                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                var response = await client.GetAsync($"http://{server.Address}:{server.Port}/v1/payload/token", cancellationToken: cancelSource.Token);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            }
            finally
            {
                server.Dispose();
                client.Dispose();
            }
        }

        [Test]
        public async Task Stop_ShouldPreventNewConnections()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();
            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");
            var client = new ClientWebSocket();

            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(10000);

            try
            {
                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);
                var serverUri = new Uri($"ws://{server.Address}:{server.Port}");

                await server.StopAsync(cancelSource.Token);

                Assert.ThrowsAsync<WebSocketException>(
                    () => client.ConnectAsync(serverUri, cancelSource.Token),
                    "Connection should be refused");
            }
            finally
            {
                client.Abort();
                client.Dispose();
                server.Dispose();
            }
        }

        [Test]
        public async Task Stop_ShouldCloseExistingConnections()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(10000);

            var mockService = new Mock<IRemoteLocalProxyService>();
            var testWebSocketServer = new TestWebSocketServer(IPAddress.Loopback, m_Logger);
            testWebSocketServer.StartAsync(cancelSource.Token);

            var server = new LocalProxyServer(m_Logger, mockService.Object, $"{testWebSocketServer.Address}:{testWebSocketServer.Port}");

            var client = new ClientWebSocket();

            var garbage = new ConcurrentBag<IDisposable>
            {
                client,
                server,
                testWebSocketServer,
            };

            try
            {
                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);
                var serverUri = new Uri($"ws://{server.Address}:{server.Port}");

                await client.ConnectAsync(serverUri, cancelSource.Token);

                // The listener is required to process the close handshake
                var listener = new WebsocketListener(
                    "client",
                    client,
                    WebSocketSource.Client,
                    m_Logger);
                garbage.Add(listener);

                await listener.StartAsync(cancelSource.Token);

                await server.StopAsync(cancelSource.Token);

                Assert.That(client.State, Is.EqualTo(WebSocketState.Closed));

                Assert.Multiple(() =>
                {
                    Assert.That(client.State, Is.EqualTo(WebSocketState.Closed).Or.EqualTo(WebSocketState.Aborted));
                    Assert.That(server.IsListening, Is.False, "Server should not be listening after being stopped");
                });
            }
            finally
            {
                foreach (var disposable in garbage)
                {
                    disposable.Dispose();
                }
            }
        }

        [Test]
        public async Task GetActiveConnections_ShouldReturnNonAbortedConnections()
        {
            var mockService = new Mock<IRemoteLocalProxyService>();
            var server = new LocalProxyServer(m_Logger, mockService.Object, "127.0.0.1:9999");
            var client1 = new ClientWebSocket();
            var client2 = new ClientWebSocket();

            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(10000);

            try
            {
                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);
                var serverUri = new Uri($"ws://{server.Address}:{server.Port}");

                await client1.ConnectAsync(serverUri, cancelSource.Token);
                await client2.ConnectAsync(serverUri, cancelSource.Token);


                IDictionary<string, WebSocket> conns = new Dictionary<string, WebSocket>();
                while (conns.Count != 2 && !cancelSource.Token.IsCancellationRequested)
                {
                    conns = server.GetAllConnections();
                    await Task.Delay(25, cancelSource.Token);
                }

                foreach (var (_, ws) in conns)
                {
                    // We implement a very simple listener, to ensure we pull the socket state
                    _ = Task.Run(
                        async () =>
                        {
                            while (ws.State == WebSocketState.Open && !cancelSource.IsCancellationRequested)
                            {
                                var buffer = new byte[1024];
                                var result = await ws.ReceiveAsync(buffer, cancelSource.Token);

                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    await ws.CloseOutputAsync(WebSocketCloseStatus.Empty, "", cancelSource.Token);
                                    ws.Dispose();
                                }
                            }
                        },
                        cancelSource.Token);
                }

                client2.Abort();

                conns = server.GetAllConnections();
                while (conns.Count != 1 && !cancelSource.Token.IsCancellationRequested)
                {
                    conns = server.GetAllConnections();
                    await Task.Delay(25, cancelSource.Token);
                }

                Assert.That(conns, Has.Count.EqualTo(1));
            }
            finally
            {
                client1.Abort();
                client1.Dispose();

                client2.Abort();
                client2.Dispose();

                server.Dispose();

                await cancelSource.CancelAsync();
            }
        }

        [Test]
        public async Task BroadcastMessage_ShouldSupportReconnect()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(20000);

            var mockService = new Mock<IRemoteLocalProxyService>();
            var server = new LocalProxyServer(m_Logger, mockService.Object, $"localhost:9999");

            var client1 = new ClientWebSocket();
            var client2 = new ClientWebSocket();


            try
            {
                await server.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);
                var serverUri = new Uri($"ws://{server.Address}:{server.Port}");

                await client1.ConnectAsync(serverUri, cancelSource.Token);

                var message = "Hello, world!"u8.ToArray();
                await server.BroadcastMessageAsync(
                    message,
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                var buffer = new byte[1024];
                var c1Result = await client1.ReceiveAsync(buffer, cancelSource.Token);
                Assert.That(buffer.AsSpan(0, c1Result.Count).ToArray(), Is.EqualTo(message));

                // Let it clean it up
                await Task.Delay(200, cancelSource.Token);

                await client2.ConnectAsync(serverUri, cancelSource.Token);

                await server.BroadcastMessageAsync(
                    message,
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                var c2Result = await client2.ReceiveAsync(buffer, cancelSource.Token);
                Assert.That(buffer.AsSpan(0, c2Result.Count).ToArray(), Is.EqualTo(message));
            }
            finally
            {
                client1.Abort();
                client1.Dispose();

                client2.Abort();
                client2.Dispose();

                server.Stop();

                await cancelSource.CancelAsync();

            }
        }
    }
}
