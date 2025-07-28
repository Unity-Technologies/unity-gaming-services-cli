namespace Unity.Services.GameServerHosting.LocalProxy.Model
{
    public enum ProxyState
    {
        None,
        Awaiting,
        Connecting,
        Connected,
        Disconnected,
        Error,
    }
}
