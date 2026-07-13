#if FEATURE_HUB_AUTH
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

class HubIpcTransport : IHubIpcTransport
{
#if USE_MOCKSERVER_ENDPOINTS
    const string k_ChannelName = "hubIPCService-test";
#else
    const string k_ChannelName = "hubIPCService";
#endif
    static readonly TimeSpan k_ConnectTimeout = TimeSpan.FromSeconds(3);

    Socket? m_Socket;
    NamedPipeClientStream? m_Pipe;
    Stream? m_Stream;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await ConnectNamedPipeAsync(cancellationToken);
        }
        else
        {
            await ConnectUnixSocketAsync(cancellationToken);
        }
    }

    public Stream GetStream()
    {
        if (m_Stream is null)
            throw new InvalidOperationException("Transport is not connected.");
        return m_Stream;
    }

    async Task ConnectUnixSocketAsync(CancellationToken cancellationToken)
    {
        var socketPath = $"/tmp/Unity-{k_ChannelName}.sock";
        m_Socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(k_ConnectTimeout);

        var endpoint = new UnixDomainSocketEndPoint(socketPath);
        await m_Socket.ConnectAsync(endpoint, timeoutCts.Token);
        m_Stream = new NetworkStream(m_Socket, ownsSocket: true);
    }

    async Task ConnectNamedPipeAsync(CancellationToken cancellationToken)
    {
        var pipeName = $"Unity-{k_ChannelName}";
        m_Pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(k_ConnectTimeout);

        await m_Pipe.ConnectAsync(timeoutCts.Token);
        m_Stream = m_Pipe;
    }

    public async ValueTask DisposeAsync()
    {
        if (m_Stream is not null)
        {
            await m_Stream.DisposeAsync();
            m_Stream = null;
        }

        m_Socket?.Dispose();
        m_Socket = null;

        if (m_Pipe is not null)
        {
            await m_Pipe.DisposeAsync();
            m_Pipe = null;
        }
    }
}
#endif
