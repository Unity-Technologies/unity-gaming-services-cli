using System;
using System.Net.WebSockets;
using System.Text;
using Unity.Services.GameServerHosting.LocalProxy.ProxyServer;

namespace Unity.Services.GameServerHosting.LocalProxy.Model
{
    public readonly struct WebsocketClientConnectedEventArgs
    {
        public string ClientId { get; }
        public WebSocket WebSocket { get; }

        public WebsocketClientConnectedEventArgs(
            string clientId,
            WebSocket webSocket
        )
        {
            ClientId = clientId;
            WebSocket = webSocket;
        }
    }

    public readonly struct WebsocketMessageReceivedEventArgs
    {
        public string ClientId { get; }
        public byte[] Message { get; }
        public WebSocketMessageType MessageType { get; }
        public WebSocketSource Source { get; }

        public WebsocketMessageReceivedEventArgs(
            string clientId,
            byte[] message,
            WebSocketMessageType messageType,
            WebSocketSource source
        )
        {
            ClientId = clientId;
            Message = message;
            MessageType = messageType;
            Source = source;
        }

        public string ReadTextMessage()
        {
            if (MessageType != WebSocketMessageType.Text)
            {
                throw new InvalidOperationException("Cannot read text message from non-text message");
            }

            return Encoding.UTF8.GetString(Message);
        }
    }

    public readonly struct ProxyStateChangedEventArgs
    {
        public ProxyState State { get; }

        public ProxyStateChangedEventArgs(ProxyState state)
        {
            State = state;
        }
    }
}
