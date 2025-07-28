using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.GameServerHosting.LocalProxy.Model;
using Unity.Services.GameServerHosting.LocalProxy.ProxyServer;
using Unity.Services.GameServerHosting.LocalProxy.UnitTest.Model;
using ILogger = Unity.Services.GameServerHosting.LocalProxy.Model.ILogger;

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.ProxyServer
{
    public class MockLogger : ILogger
    {
        public List<string> LoggedErrors { get; } = new();
        public List<string> LoggedLogs { get; } = new();
        public List<string> LoggedVerbose { get; } = new();
        public List<string> LoggedDebug { get; } = new();
        public List<string> LoggedInfo { get; } = new();
        public List<string> LoggedWarns { get; } = new();

        public void Error(string message, params object[] args)
        {
            LoggedErrors.Add(string.Format(message, args));
        }

        public void Log(LogLevel level, string message, params object[] args)
        {
            LoggedLogs.Add(string.Format(message, args));
        }

        public void Verbose(string message, params object[] args)
        {
            LoggedVerbose.Add(string.Format(message, args));
        }

        public void Debug(string message, params object[] args)
        {
            LoggedDebug.Add(string.Format(message, args));
        }
        public void Info(string message, params object[] args)
        {
            LoggedInfo.Add(string.Format(message, args));
        }

        public void Warn(string message, params object[] args)
        {
            LoggedWarns.Add(string.Format(message, args));
        }
    }

    public class WebsocketListenerTests
    {
        ILogger m_Logger;
        const string k_ShortMessage = "Hello, world!";

        const string k_LongMessage =
            @"Cupcake ipsum dolor sit amet candy canes. Tart pastry tootsie roll liquorice marzipan. Macaroon I love bonbon I love sugar plum candy gummies tiramisu biscuit. Powder tart dragée fruitcake I love gingerbread soufflé croissant dragée. I love donut I love chocolate bar soufflé croissant soufflé. Bonbon marzipan jelly beans dessert cookie danish. Jelly cheesecake shortbread apple pie croissant sesame snaps. Cupcake gummies sweet roll donut lemon drops. Cupcake ice cream jelly beans cupcake sugar plum I love candy chocolate muffin. Toffee I love toffee I love shortbread bonbon sugar plum gingerbread. Liquorice candy canes donut ice cream cake caramels cake oat cake. Jujubes wafer chupa chups pastry cookie. Wafer I love cookie pie liquorice lollipop ice cream jelly beans dessert.

I love marshmallow jelly beans fruitcake sweet. Candy canes I love halvah cake pastry cake liquorice I love gummi bears. Cake shortbread bonbon bonbon toffee pastry cake biscuit. Cookie candy canes soufflé croissant jujubes muffin chupa chups I love. Toffee sweet roll donut danish bear claw. Brownie cotton candy cake pastry I love jelly-o apple pie chocolate cheesecake. Croissant I love halvah lemon drops croissant ice cream. Chupa chups tart soufflé macaroon cotton candy caramels I love shortbread marshmallow. Chocolate shortbread pie toffee croissant donut I love tiramisu chocolate. Shortbread cookie marzipan cake cookie carrot cake cookie. Soufflé sweet fruitcake soufflé sesame snaps pastry I love. Chocolate cupcake I love lemon drops I love jelly-o I love I love cookie. Fruitcake caramels sugar plum icing carrot cake muffin. Liquorice caramels bear claw lollipop chocolate croissant chupa chups cotton candy cheesecake.

Chupa chups toffee pastry I love donut. Dragée chupa chups sweet roll pastry jujubes tiramisu bonbon cookie cake. Soufflé marshmallow bear claw topping chupa chups cheesecake brownie sesame snaps. Cake croissant bear claw I love chocolate. Gummi bears toffee marzipan icing pie sweet pie. Sugar plum ice cream cheesecake cookie sesame snaps. Chupa chups muffin oat cake cake cookie marshmallow cheesecake muffin. Jelly candy canes bonbon candy canes brownie. Caramels I love jelly beans danish tootsie roll. Marshmallow pie I love gummies brownie brownie sugar plum marzipan. Toffee biscuit ice cream jelly donut. Sesame snaps shortbread cotton candy cake bonbon danish cotton candy liquorice donut.

Cake jujubes wafer danish chupa chups. Liquorice soufflé halvah biscuit sweet chocolate ice cream. Candy fruitcake caramels shortbread candy canes chocolate bar. Gummi bears biscuit croissant jujubes cookie bonbon cheesecake jelly beans gummi bears. Gingerbread I love liquorice tootsie roll brownie tiramisu gingerbread candy canes. Jelly beans apple pie pudding sesame snaps apple pie. I love cookie sweet jujubes topping. Carrot cake pastry jelly-o fruitcake shortbread shortbread jujubes tootsie roll. Tootsie roll chocolate muffin topping I love dessert cake cake. Halvah macaroon cake bonbon ice cream chocolate sugar plum jelly beans pudding. I love lemon drops jelly toffee croissant lemon drops bonbon. Gummi bears sesame snaps danish marshmallow brownie cake.

Topping pie I love gummi bears cupcake chocolate brownie. Marzipan macaroon chupa chups muffin dragée chocolate jelly I love carrot cake. Sesame snaps dessert ice cream I love caramels jelly. Tiramisu cake chocolate cake liquorice I love lemon drops bear claw tiramisu tiramisu. Chupa chups tart I love biscuit croissant. Brownie macaroon dessert liquorice cupcake marzipan lollipop. Gummies candy biscuit cake apple pie marshmallow oat cake carrot cake. Marshmallow I love topping macaroon gummi bears marzipan carrot cake dessert cotton candy. Bonbon I love donut halvah sugar plum gummi bears I love. Cupcake I love I love toffee liquorice soufflé halvah lollipop. Chupa chups cupcake dessert chupa chups I love halvah sugar plum chocolate pastry. Macaroon cupcake marzipan sesame snaps marshmallow cheesecake. Biscuit jelly-o liquorice muffin chocolate cake fruitcake. Pie bonbon chocolate cake icing icing cake candy.";

        [SetUp]
        public void SetUp()
        {
            m_Logger = new NilLogger();
        }

        [Test]
        public async Task Start_ShouldRespondToCloseMessages()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var server = new TestWebSocketServer(IPAddress.Loopback, m_Logger);

            server.StartAsync(cancelSource.Token);

            var serverConnection = new ClientWebSocket();
            await serverConnection.ConnectAsync(
                new Uri($"ws://{server.Address}:{server.Port}"),
                cancelSource.Token);


            var listener = new WebsocketListener(
                "test",
                serverConnection,
                WebSocketSource.Client,
                m_Logger);

            try
            {
                await listener.StartAsync(cancelSource.Token);
                Assert.That(listener.State, Is.EqualTo(ListenerState.Running), "listener must be in running state");

                await serverConnection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancelSource.Token);

                Assert.That(serverConnection.State, Is.EqualTo(WebSocketState.Closed), "Server connection must be closed");
            }
            finally
            {
                listener.Dispose();

                serverConnection.Abort();
                serverConnection.Dispose();

                server.Stop();

                await cancelSource.CancelAsync();
            }
        }

        [Test]
        public async Task Start_ShouldListenForShortMessages()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var streamPair = TestNetworkStream.CreatePair();

            var client = WebSocket.CreateFromStream(
                streamPair.ClientStream,
                false,
                null,
                Timeout.InfiniteTimeSpan);
            Assert.That(client.State, Is.EqualTo(WebSocketState.Open), "client must be in open state");

            var server = WebSocket.CreateFromStream(
                streamPair.ServerStream,
                true,
                null,
                Timeout.InfiniteTimeSpan);
            Assert.That(server.State, Is.EqualTo(WebSocketState.Open), "server must be in open state");

            var listener = new WebsocketListener(
                "test",
                server,
                WebSocketSource.Client,
                m_Logger);

            Queue<string> receivedMessages = new();
            listener.MessageReceived += (_, args) =>
            {
                receivedMessages.Enqueue(args.ReadTextMessage());
            };

            try
            {
                await listener.StartAsync(cancelSource.Token);
                Assert.That(listener.State, Is.EqualTo(ListenerState.Running), "listener must be in running state");

                var buffer = Encoding.UTF8.GetBytes(k_ShortMessage);
                await client.SendAsync(
                    buffer,
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                var hasMessage = false;
                while (!hasMessage && !cancelSource.IsCancellationRequested)
                {
                    await Task.Delay(100, cancelSource.Token);

                    hasMessage = receivedMessages.Count > 0;
                }

                Assert.Multiple(
                    () =>
                    {
                        Assert.That(receivedMessages, Has.Count.EqualTo(1));
                        var receivedMsg = receivedMessages.Dequeue();
                        Assert.That(
                            receivedMsg,
                            Is.EqualTo(k_ShortMessage),
                            "Received message does not match sent message");
                    });
            }
            finally
            {
                listener.Dispose();

                client.Abort();
                client.Dispose();

                server.Abort();
                server.Dispose();

                await streamPair.ClientStream.DisposeAsync();
                await streamPair.ServerStream.DisposeAsync();

                await cancelSource.CancelAsync();
            }
        }

        [Test]
        public async Task Start_ShouldListenForLongMessages()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var streamPair = TestNetworkStream.CreatePair();

            var client = WebSocket.CreateFromStream(
                streamPair.ClientStream,
                false,
                null,
                Timeout.InfiniteTimeSpan);
            Assert.That(client.State, Is.EqualTo(WebSocketState.Open), "client must be in open state");

            var server = WebSocket.CreateFromStream(
                streamPair.ServerStream,
                true,
                null,
                Timeout.InfiniteTimeSpan);
            Assert.That(server.State, Is.EqualTo(WebSocketState.Open), "server must be in open state");

            var listener = new WebsocketListener(
                "test",
                server,
                WebSocketSource.Client,
                m_Logger,
                128);

            Queue<string> receivedMessages = new();
            listener.MessageReceived += (_, args) =>
            {
                receivedMessages.Enqueue(args.ReadTextMessage());
            };

            try
            {
                await listener.StartAsync(cancelSource.Token);
                Assert.That(listener.State, Is.EqualTo(ListenerState.Running), "listener must be in running state");

                var buffer = Encoding.UTF8.GetBytes(k_LongMessage);
                await client.SendAsync(
                    buffer,
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                var hasMessage = false;
                while (!hasMessage && !cancelSource.IsCancellationRequested)
                {
                    await Task.Delay(100, cancelSource.Token);

                    hasMessage = receivedMessages.Count > 0;
                }

                Assert.Multiple(
                    () =>
                    {
                        Assert.That(receivedMessages, Has.Count.EqualTo(1));
                        var receivedMsg = receivedMessages.Dequeue();
                        Assert.That(
                            receivedMsg,
                            Is.EqualTo(k_LongMessage),
                            "Received message does not match sent message");
                    });
            }
            finally
            {
                listener.Dispose();

                client.Abort();
                client.Dispose();

                server.Abort();
                server.Dispose();

                await streamPair.ClientStream.DisposeAsync();
                await streamPair.ServerStream.DisposeAsync();

                await cancelSource.CancelAsync();
            }
        }


        [Test]
        public async Task Start_ShouldHandleExceptions()
        {
            using var cancelSource = new CancellationTokenSource();
            cancelSource.CancelAfter(30000);

            var streamPair = TestNetworkStream.CreatePair();

            var client = WebSocket.CreateFromStream(
                streamPair.ClientStream,
                false,
                null,
                Timeout.InfiniteTimeSpan);
            Assert.That(client.State, Is.EqualTo(WebSocketState.Open), "client must be in open state");

            var server = WebSocket.CreateFromStream(
                streamPair.ServerStream,
                true,
                null,
                Timeout.InfiniteTimeSpan);
            Assert.That(server.State, Is.EqualTo(WebSocketState.Open), "server must be in open state");

            var mockLogger = new MockLogger();
            var listener = new WebsocketListener(
                "test",
                server,
                WebSocketSource.Client,
                mockLogger);

            // Simulate an exception in the WebSocket
            listener.MessageReceived += (_, _) => throw new InvalidOperationException("Simulated exception");

            try
            {
                await listener.StartAsync(cancelSource.Token);
                Assert.That(listener.State, Is.EqualTo(ListenerState.Running), "listener must be in running state");

                var buffer = Encoding.UTF8.GetBytes(k_ShortMessage);
                await client.SendAsync(
                    buffer,
                    WebSocketMessageType.Text,
                    true,
                    cancelSource.Token);

                if (!await Wait.ForConditionAsync(
                            () => mockLogger.LoggedErrors.Count > 0,
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromMilliseconds(100)))
                {
                    Assert.Fail("Timeout waiting for exception to be logged");
                }

                Assert.That(
                    mockLogger.LoggedErrors,
                    Has.Some.Contains("Simulated exception"),
                    "The exception should be logged");
            }
            finally
            {
                listener.Dispose();

                client.Abort();
                client.Dispose();

                server.Abort();
                server.Dispose();

                await streamPair.ClientStream.DisposeAsync();
                await streamPair.ServerStream.DisposeAsync();

                await cancelSource.CancelAsync();
            }
        }
    }
}
