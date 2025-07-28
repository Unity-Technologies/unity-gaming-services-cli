using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
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

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.ProxyServer
{
    public class LocalProxyTests
    {
        const string k_ClientMessage = "Message from the client";
        const string k_UpstreamMessage = "Message from the upstream server";

        ILogger m_Logger;
        ILogger m_TestLogger;

        [SetUp]
        public void Setup()
        {
            m_Logger = new TestLogger();
            m_TestLogger = new TestLogger();
        }

        [Test]
        public async Task Start_ShouldProxyMessages()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(50000);

            var garbage = new ConcurrentBag<IDisposable>();

            // Using the WebsocketServer itself to test the proxy is far from ideal, but we need something that implements
            // the WebSocket protocol to test the proxy. We could use a library like Fleck or ASP.NET, but that would require a lot of
            // setup. We just need to set up this upstream to not make any forward connections or HTTP calls.
            var noopService = new Mock<IRemoteLocalProxyService>();
            var upstreamServer = new LocalProxyServer(m_Logger, noopService.Object, "127.0.0.1:9999");
            var upstreamMessages = new ConcurrentQueue<string>();
            upstreamServer.ClientConnected += (_, args) =>
            {
                var listener = new WebsocketListener(
                    args.ClientId,
                    args.WebSocket,
                    WebSocketSource.Client,
                    m_Logger);

                listener.MessageReceived += (_, msgArg) =>
                {
                    upstreamMessages.Enqueue(msgArg.ReadTextMessage());
                    if (msgArg.MessageType == WebSocketMessageType.Close)
                    {
                        listener.Stop();
                    }
                };

                garbage.Add(listener);
                listener.Start();
            };
            garbage.Add(upstreamServer);

            var mockService = new Mock<IRemoteLocalProxyService>();
            mockService.Reset();

            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);
            garbage.Add(proxy);

            var wsClient = new ClientWebSocket();
            var clientListener = new WebsocketListener(
                "client",
                wsClient,
                WebSocketSource.Client,
                m_Logger);
            var clientMessages = new ConcurrentQueue<string>();
            clientListener.MessageReceived += (_, args) =>
            {
                clientMessages.Enqueue(args.ReadTextMessage());
            };

            garbage.Add(clientListener);
            garbage.Add(wsClient);

            try
            {
                await upstreamServer.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                await proxy.StartAsync(
                    new LocalProxyConfig()
                    {
                        UpstreamHost = $"{upstreamServer.Address}:{upstreamServer.Port}",
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                    },
                    cancelSource.Token);

                if (proxy.ProxyState is not ProxyState.Connected)
                {
                    TestContext.WriteLine("Proxy is in {0} state: {1}", proxy.ProxyState, proxy.DisconnectReason);
                }

                Assert.That(proxy.ProxyState, Is.EqualTo(ProxyState.Awaiting), "proxy should be in awaiting state");

                await wsClient.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);
                await clientListener.StartAsync(cancelSource.Token);

                if (!await Wait.ForConditionAsync(
                        () => proxy.ProxyState == ProxyState.Connected,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail("LocalProxy doesn't change state to Connected after the client connected");
                }

                // We emit from the client
                await wsClient.SendAsync(
                    Encoding.UTF8.GetBytes(k_ClientMessage),
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                // We emit from the upstream server
                await upstreamServer.BroadcastMessageAsync(
                    Encoding.UTF8.GetBytes(k_UpstreamMessage),
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                var hasMessage = false;
                while (!hasMessage && !cancelSource.IsCancellationRequested)
                {
                    await Task.Delay(50, cancelSource.Token);
                    hasMessage = !upstreamMessages.IsEmpty && !clientMessages.IsEmpty;
                }

                Assert.Multiple(() =>
                {
                    Assert.That(upstreamMessages, Has.Count.EqualTo(1));
                    Assert.That(clientMessages, Has.Count.EqualTo(1));

                    Assert.That(upstreamMessages.TryDequeue(out var upstreamMsg), Is.True);
                    Assert.That(clientMessages.TryDequeue(out var clientMsg), Is.True);

                    Assert.That(
                        upstreamMsg,
                        Is.EqualTo(k_ClientMessage),
                        "Upstream message does not match client message");
                    Assert.That(
                        clientMsg,
                        Is.EqualTo(k_UpstreamMessage),
                        "Client message does not match upstream message");
                });
            }
            finally
            {
                await cancelSource.CancelAsync();

                foreach (var disposable in garbage)
                {
                    disposable.Dispose();
                }
            }
        }

        [Test]
        public async Task Start_ShouldInterceptMessages()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var garbage = new ConcurrentBag<IDisposable>();

            // Using the WebsocketServer itself to test the proxy is far from ideal, but we need something that implements
            // the WebSocket protocol to test the proxy. We could use a library like Fleck or ASP.NET, but that would require a lot of
            // setup. We just need to set up this upstream to not make any forward connections or HTTP calls.
            var noopService = new Mock<IRemoteLocalProxyService>();
            var upstreamServer = new LocalProxyServer(m_TestLogger, noopService.Object, "127.0.0.1:9999");
            upstreamServer.ClientConnected += (_, args) =>
            {
                var listener = new WebsocketListener(
                    args.ClientId,
                    args.WebSocket,
                    WebSocketSource.Client,
                    m_TestLogger);

                garbage.Add(listener);
                listener.Start();
            };
            garbage.Add(upstreamServer);

            var mockService = new Mock<IRemoteLocalProxyService>();

            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_TestLogger, mockService.Object);
            garbage.Add(proxy);

            var messagedBySource = new Dictionary<WebSocketSource, ConcurrentBag<string>>
            {
                [WebSocketSource.Client] = new(),
                [WebSocketSource.Upstream] = new(),
            };
            proxy.MessageReceived += (_, args) =>
            {
                messagedBySource[args.Source].Add(args.ReadTextMessage());
            };

            var wsClient = new ClientWebSocket();
            var clientListener = new WebsocketListener(
                "client",
                wsClient,
                WebSocketSource.Client,
                m_TestLogger);

            garbage.Add(clientListener);
            garbage.Add(wsClient);

            try
            {
                await upstreamServer.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                await proxy.StartAsync(
                    new LocalProxyConfig
                    {
                        UpstreamHost = $"{upstreamServer.Address}:{upstreamServer.Port}",
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                        ClientAuth = new WebsocketClientAuth("dummy", "dummy")
                    },
                    cancelSource.Token);

                if (proxy.ProxyState is not ProxyState.Connected)
                {
                    TestContext.WriteLine("Proxy is in {0} state: {1}", proxy.ProxyState, proxy.DisconnectReason);
                }

                Assert.That(proxy.ProxyState, Is.EqualTo(ProxyState.Awaiting), "proxy should be in awaiting state");

                await wsClient.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);
                await clientListener.StartAsync(cancelSource.Token);

                if (!await Wait.ForConditionAsync(
                        () => proxy.ProxyState == ProxyState.Connected,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail("LocalProxy doesn't change state to Connected after the client connected");
                }

                // We emit from the client
                await wsClient.SendAsync(
                    Encoding.UTF8.GetBytes(k_ClientMessage),
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                // We emit from the upstream server
                await upstreamServer.BroadcastMessageAsync(
                    Encoding.UTF8.GetBytes(k_UpstreamMessage),
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                var hasMessages = false;
                while (!hasMessages && !cancelSource.IsCancellationRequested)
                {
                    await Task.Delay(50, cancelSource.Token);
                    hasMessages = !messagedBySource[WebSocketSource.Client].IsEmpty &&
                                  !messagedBySource[WebSocketSource.Upstream].IsEmpty;
                }

                Assert.Multiple(() =>
                {
                    Assert.That(messagedBySource[WebSocketSource.Client], Has.Count.EqualTo(1));
                    Assert.That(messagedBySource[WebSocketSource.Upstream], Has.Count.EqualTo(1));

                    Assert.That(messagedBySource[WebSocketSource.Client].TryTake(out var clientMsg), Is.True);
                    Assert.That(messagedBySource[WebSocketSource.Upstream].TryTake(out var upstreamMsg), Is.True);

                    Assert.That(clientMsg, Is.EqualTo(k_ClientMessage), "Client message does not match sent message");
                    Assert.That(
                        upstreamMsg,
                        Is.EqualTo(k_UpstreamMessage),
                        "Upstream message does not match sent message");
                });
            }
            finally
            {
                await cancelSource.CancelAsync();

                foreach (var disposable in garbage)
                {
                    disposable.Dispose();
                }
            }
        }

        [Test]
        public async Task Start_ShouldReportState()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var noopService = new Mock<IRemoteLocalProxyService>();
            var upstreamServer = new LocalProxyServer(m_Logger, noopService.Object, "127.0.0.1:9999");

            var proxyStatus = new Stack<ProxyState>();
            var mockService = new Mock<IRemoteLocalProxyService>();
            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);
            proxy.ProxyStateChanged += (_, args) =>
            {
                proxyStatus.Push(args.State);
            };

            try
            {
                await upstreamServer.StartAsync(IPAddress.Loopback, 0, cancelSource.Token);

                await proxy.StartAsync(
                    new LocalProxyConfig
                    {
                        UpstreamHost = $"{upstreamServer.Address}:{upstreamServer.Port}",
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                    },
                    cancelSource.Token);

                Assert.That(proxy.ProxyState, Is.EqualTo(ProxyState.Awaiting), "proxy should be awaiting now");

                var wsClient = new ClientWebSocket();
                await wsClient.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);

                if (!await Wait.ForConditionAsync(
                        () => proxy.ProxyState == ProxyState.Connected,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail("LocalProxy doesn't change state to Connected after the client connected");
                }

                // We emit from the client
                await wsClient.SendAsync(
                    Encoding.UTF8.GetBytes(k_ClientMessage),
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                wsClient.Abort();

                proxy.Stop();

                var isProxyClosed = false;
                while (!isProxyClosed && !cancelSource.IsCancellationRequested)
                {
                    await Task.Delay(50, cancelSource.Token);
                    isProxyClosed = proxy.ProxyState is ProxyState.Disconnected;
                }

                Assert.Multiple(() =>
                {
                    // The way we watch for events, it is very well possible we miss the "Connecting" state. We don't test
                    // for it, but it should technically be possible to see it.
                    Assert.That(proxyStatus, Has.Count.GreaterThanOrEqualTo(2));
                    Assert.That(proxyStatus.Pop(), Is.EqualTo(ProxyState.Disconnected));
                    Assert.That(proxyStatus.Pop(), Is.EqualTo(ProxyState.Connected));
                });
            }
            finally
            {
                await cancelSource.CancelAsync();

                upstreamServer.Dispose();
                proxy.Dispose();
            }
        }

        [Test]
        public async Task Start_WhenUpstreamCannotConnectShouldErrorOut()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var mockService = new Mock<IRemoteLocalProxyService>();
            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);
            var proxyStateStack = new Stack<ProxyState>();

            proxy.ProxyStateChanged += (_, args) =>
            {
                proxyStateStack.Push(args.State);
            };
            var proxyArgs = new LocalProxyConfig
            {
                UpstreamHost = "localhost:19965",
                Insecure = true,
                ServerAddress = IPAddress.Loopback,
                ServerPort = 0,
            };

            try
            {
                await proxy.StartAsync(proxyArgs, cancelSource.Token);

                var wsClient = new ClientWebSocket();
                await wsClient.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);

                if (!await Wait.ForConditionAsync(
                        () => proxyStateStack.Count >= 2,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail("Expected at least 2 states in the stack, but got: {0}", proxyStateStack.Count);
                }

                Assert.Multiple(() =>
                {
                    Assert.That(proxyStateStack.Pop(), Is.EqualTo(ProxyState.Error));
                    Assert.That(proxyStateStack.Pop(), Is.EqualTo(ProxyState.Awaiting));
                    Assert.That(
                        proxy.DisconnectReason,
                        Is.EqualTo(
                            $"Unable to connect to upstream server: ws://{proxyArgs.UpstreamHost}/v1/connection/websocket: Unable to connect to the remote server"));
                });
            }
            finally
            {
                await cancelSource.CancelAsync();
                proxy.Dispose();
            }
        }

        [Test]
        public async Task Start_WhenUpstreamDisconnectsShouldDisconnect()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(10000);

            var upstreamServerSimple = new TestWebSocketServer(IPAddress.Loopback, m_Logger);
            upstreamServerSimple.StartAsync(cancelSource.Token);

            var mockService = new Mock<IRemoteLocalProxyService>();
            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);

            try
            {
                await proxy.StartAsync(
                    new LocalProxyConfig
                    {
                        UpstreamHost = $"{upstreamServerSimple.Address}:{upstreamServerSimple.Port}",
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                    },
                    cancelSource.Token);

                var wsClient = new ClientWebSocket();
                await wsClient.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);

                if (!await Wait.ForConditionAsync(
                        () => proxy.ProxyState == ProxyState.Connected,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail("LocalProxy doesn't change state to Connected after the client connected");
                }

                // We stop the upstream server to cause the proxy to disconnect too.
                upstreamServerSimple.Stop();


                // Waiting until the proxy disconnects
                var isProxyDisconnected = false;
                while (!isProxyDisconnected && !cancelSource.IsCancellationRequested)
                {
                    await Task.Delay(50, cancelSource.Token);
                    isProxyDisconnected = proxy.ProxyState is ProxyState.Disconnected;
                }

                Assert.Multiple(() =>
                {
                    Assert.That(proxy.ProxyState, Is.EqualTo(ProxyState.Disconnected));
                    Assert.That(
                        proxy.DisconnectReason,
                        Is.EqualTo("Either client or upstream disconnected gracefully"));
                });
            }
            finally
            {
                await cancelSource.CancelAsync();
                proxy.Stop();
                upstreamServerSimple.Stop();
            }
        }

        [Test]
        public async Task Start_ShouldErrorWhenUpstreamURINull()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var mockService = new Mock<IRemoteLocalProxyService>();
            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);

            try
            {
                await proxy.StartAsync(
                    new LocalProxyConfig
                    {
                        UpstreamHost = null,
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                    },
                    cancelSource.Token);

                // Should not get here
                Assert.Fail();
            }
            catch (ArgumentNullException argNullException)
            {
                StringAssert.Contains("Upstream host must be set", argNullException.Message);
            }
            finally
            {
                await cancelSource.CancelAsync();
                proxy.Stop();
            }
        }

        [Test]
        public async Task Start_ShouldSupportReconnect()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(50000);

            var garbage = new ConcurrentBag<IDisposable>();

            var testWebSocketServer = new TestWebSocketServer(IPAddress.Loopback, m_Logger);

            testWebSocketServer.StartAsync(cancelSource.Token);
            garbage.Add(testWebSocketServer);

            var mockService = new Mock<IRemoteLocalProxyService>();
            mockService.Reset();

            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);
            proxy.ProxyStateChanged += (sender, args) => m_Logger.Debug("Proxy state changed to {0}", args.State);
            garbage.Add(proxy);

            var wsClient1 = new ClientWebSocket();
            garbage.Add(wsClient1);
            var wsClient2 = new ClientWebSocket();
            garbage.Add(wsClient2);

            try
            {

                await proxy.StartAsync(
                    new LocalProxyConfig()
                    {
                        UpstreamHost = $"{testWebSocketServer.Address}:{testWebSocketServer.Port}",
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                    },
                    cancelSource.Token);

                Assert.That(proxy.ProxyState, Is.EqualTo(ProxyState.Awaiting), "proxy should be in awaiting state");

                await wsClient1.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);


                if (!await Wait.ForConditionAsync(
                        () => proxy.ProxyState == ProxyState.Connected,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail($"LocalProxy doesn't change state to Connected after the first connected. State: {proxy.ProxyState}");
                }

                wsClient1.Abort();

                if (!await Wait.ForConditionAsync(
                        () => proxy.ProxyState == ProxyState.Awaiting,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail($"LocalProxy doesn't change state to Awaiting after the first disconnected. State: {proxy.ProxyState}");
                }

                await wsClient2.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);

                if (!await Wait.ForConditionAsync(
                        () => proxy.ProxyState == ProxyState.Connected,
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail($"LocalProxy doesn't change state to Connected after the client reconnected. State: {proxy.ProxyState}");
                }

                wsClient2.Abort();

                if (!await Wait.ForConditionAsync(
                    () => proxy.ProxyState == ProxyState.Awaiting,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromMilliseconds(50)))
                {
                    Assert.Fail("LocalProxy doesn't change state to Awaiting after the client disconnected");
                }
            }
            finally
            {
                await cancelSource.CancelAsync();

                foreach (var disposable in garbage)
                {
                    disposable.Dispose();
                }
            }
        }

        [Test]
        public async Task Start_ShouldThrownAnExceptionIfStartedTwice()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(50000);

            var garbage = new ConcurrentBag<IDisposable>();

            var testWebSocketServer = new TestWebSocketServer(IPAddress.Loopback, m_Logger);

            testWebSocketServer.StartAsync(cancelSource.Token);
            garbage.Add(testWebSocketServer);

            var mockService = new Mock<IRemoteLocalProxyService>();
            mockService.Reset();

            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);
            proxy.ProxyStateChanged += (sender, args) => m_Logger.Debug("Proxy state changed to {0}", args.State);
            garbage.Add(proxy);

            try
            {

                await proxy.StartAsync(
                    new LocalProxyConfig()
                    {
                        UpstreamHost = $"{testWebSocketServer.Address}:{testWebSocketServer.Port}",
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                    },
                    cancelSource.Token);

                Assert.That(proxy.ProxyState, Is.EqualTo(ProxyState.Awaiting), "proxy should be in awaiting state");

                Assert.Throws<InvalidOperationException>(() =>
                {
                    proxy.StartAsync(
                        new LocalProxyConfig()
                        {
                            UpstreamHost = $"{testWebSocketServer.Address}:{testWebSocketServer.Port}",
                            Insecure = true,
                            ServerAddress = IPAddress.Loopback,
                            ServerPort = 0,
                        },
                        cancelSource.Token).GetAwaiter().GetResult();
                }, "Starting the proxy twice should throw an InvalidOperationException");

            }
            finally
            {
                await cancelSource.CancelAsync();

                foreach (var disposable in garbage)
                {
                    disposable.Dispose();
                }
            }
        }

        [Test]
        public async Task Start_ShouldCloseSecondClient()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(50000);

            var garbage = new ConcurrentBag<IDisposable>();

            var testWebSocketServer = new TestWebSocketServer(IPAddress.Loopback, m_Logger);

            testWebSocketServer.StartAsync(cancelSource.Token);
            garbage.Add(testWebSocketServer);

            var mockService = new Mock<IRemoteLocalProxyService>();
            mockService.Reset();

            var proxy = new LocalProxy.ProxyServer.LocalProxy(m_Logger, mockService.Object);
            proxy.ProxyStateChanged += (sender, args) => m_Logger.Debug("Proxy state changed to {0}", args.State);
            garbage.Add(proxy);

            var wsClient1 = new ClientWebSocket();
            garbage.Add(wsClient1);
            var wsClient2 = new ClientWebSocket();
            garbage.Add(wsClient2);

            try
            {

                await proxy.StartAsync(
                    new LocalProxyConfig()
                    {
                        UpstreamHost = $"{testWebSocketServer.Address}:{testWebSocketServer.Port}",
                        Insecure = true,
                        ServerAddress = IPAddress.Loopback,
                        ServerPort = 0,
                    },
                    cancelSource.Token);

                Assert.That(proxy.ProxyState, Is.EqualTo(ProxyState.Awaiting), "proxy should be in awaiting state");

                await wsClient1.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);

                await wsClient2.ConnectAsync(
                    new Uri($"ws://{proxy.ServerAddress}:{proxy.ServerPort}"),
                    cancelSource.Token);

                Wait.ForConditionAsync(
                    () => wsClient2.State == WebSocketState.Closed,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromMilliseconds(50)).GetAwaiter().GetResult();

            }
            finally
            {
                await cancelSource.CancelAsync();

                foreach (var disposable in garbage)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
