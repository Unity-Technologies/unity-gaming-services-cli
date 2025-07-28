using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.GameServerHosting.LocalProxy.Model;

namespace Unity.Services.GameServerHosting.LocalProxy.ProxyServer
{
    public enum WebSocketSource { Client, Upstream }

    public enum ListenerState { None, Starting, Running, Stopped }

    /// <summary>
    /// Listens for messages on a WebSocket connection and raises events when messages are received through `MessageReceived`.
    /// </summary>
    public sealed class WebsocketListener : IDisposable
    {
        const int k_MessageBufferSizeFactor = 10;

        readonly string m_Id;
        readonly WebSocket m_Websocket;
        readonly ILogger m_Logger;

        /// <summary>
        /// Just to know if it's an Upstream or Client connection
        /// </summary>
        readonly WebSocketSource m_SourceType;

        readonly CancellationTokenSource m_CancelSource = new();

        readonly byte[] m_FrameBuffer;
        byte[] m_MessageBuffer;
        int m_MessageBufferCursor;

        /// <summary>
        /// Event triggered when a message is received from the WebSocket connection.
        ///
        /// Only for Binary and Text messages. Close messages are handled internally.
        /// </summary>
        public event EventHandler<WebsocketMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// State of the listener
        /// </summary>
        public ListenerState State { get; private set; } = ListenerState.None;

        public WebsocketListener(
            string id,
            WebSocket ws,
            WebSocketSource sourceType,
            ILogger logger,
            int bufferSize = 1024
        )
        {
            m_Id = id;
            m_Websocket = ws;
            m_Logger = logger;
            m_SourceType = sourceType;

            m_FrameBuffer = new byte[bufferSize];
            m_MessageBuffer = new byte[k_MessageBufferSizeFactor * bufferSize];
            m_MessageBufferCursor = 0;
        }

        /// <summary>
        /// Starts the listener in a new thread.
        /// </summary>
        public void Start()
        {
            if (State != ListenerState.None || m_CancelSource.IsCancellationRequested)
            {
                throw new InvalidOperationException("The listener is already started or has been disposed");
            }

            State = ListenerState.Starting;

            // We launch the listener in a new thread
            Run(m_CancelSource.Token)
                .ContinueWith(
                    task =>
                    {
                        if (task.Exception != null)
                        {
                            m_Logger.Error(
                                "Unhandled exception in WebSocket listener: {0}",
                                task.Exception.Flatten().InnerException);
                        }
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Starts the listener in a new thread and waits for it to be ready.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            Start();

            using var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var isRunningOrStopped = State is ListenerState.Running or ListenerState.Stopped;

            while (!isRunningOrStopped && !cancelSource.Token.IsCancellationRequested)
            {
                await Task.Delay(50, cancelSource.Token);
                isRunningOrStopped = State is ListenerState.Running or ListenerState.Stopped;
            }
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public void Stop()
        {
            m_CancelSource.Cancel();
        }

        /// <summary>
        /// Stops and disposes the listener.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Runs the listener until the CancellationToken provided is requesting a cancellation.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        async Task Run(CancellationToken cancellationToken)
        {
            while (m_Websocket.State is WebSocketState.None or WebSocketState.Connecting)
            {
                m_Logger.Debug("{0} Connecting", m_SourceType);
                await Task.Delay(50, cancellationToken);
            }

            while (m_Websocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                State = ListenerState.Running;

                var result = await ReadBytes(cancellationToken);
                if (result == null) continue;
                if (!result.EndOfMessage)
                {
                    // We read the full message before raising the event. This proxy does not handle fragmented messages.
                    continue;
                }

                m_Logger.Debug(
                    "Received a {0} message from {1}: <{2}> {3}",
                    result.MessageType,
                    m_SourceType,
                    result.CloseStatus ?? WebSocketCloseStatus.Empty,
                    result.CloseStatusDescription ?? string.Empty
                );
                OnMessageReceived(m_MessageBuffer, m_MessageBufferCursor, result.MessageType);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    m_Logger.Debug("Received Close message. We need to response back to {0}", m_SourceType);

                    await m_Websocket.CloseAsync(
                        result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription ?? string.Empty,
                        cancellationToken
                    );
                }

                m_MessageBufferCursor = 0;
            }

            State = ListenerState.Stopped;
        }

        /// <summary>
        /// Reads the bytes from the WebSocket and loads them into the message buffer, through the frame buffer.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The result from the WebSocket data reception</returns>
        async Task<WebSocketReceiveResult> ReadBytes(CancellationToken cancellationToken)
        {
            WebSocketReceiveResult result = null;

            try
            {
                result = await m_Websocket.ReceiveAsync(m_FrameBuffer, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Do nothing. This is expected when the listener is stopped.
            }
            catch (Exception ex)
            {
                m_Logger.Error("Error receiving message from {0}: {1}", m_SourceType, ex.Message);
            }

            if (result == null)
            {
                return null;
            }

            // If the message is larger than the current message buffer, we need to resize it.
            if (m_MessageBufferCursor + result.Count >= m_MessageBuffer.Length)
            {
                Array.Resize(ref m_MessageBuffer, m_MessageBuffer.Length * 2);
            }

            // We write the frame buffer to the message buffer
            m_FrameBuffer.AsSpan(0, result.Count).CopyTo(m_MessageBuffer.AsSpan(m_MessageBufferCursor));
            m_MessageBufferCursor += result.Count;

            return result;
        }

        void OnMessageReceived(byte[] messageBuffer, int length, WebSocketMessageType messageType)
        {
            var message = messageBuffer.AsSpan(0, length).ToArray();

            MessageReceived?.Invoke(
                this,
                new WebsocketMessageReceivedEventArgs(
                    m_Id,
                    message,
                    messageType,
                    m_SourceType
                ));
        }
    }
}
