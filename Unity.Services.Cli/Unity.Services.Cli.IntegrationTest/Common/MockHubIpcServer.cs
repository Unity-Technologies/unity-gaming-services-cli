using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Cli.IntegrationTest.Common;

/// <summary>
/// A fake Unity Hub IPC server that speaks the form-feed-delimited JSON protocol.
/// Listens on the test channel name used when USE_MOCKSERVER_ENDPOINTS is defined.
/// </summary>
class MockHubIpcServer : IAsyncDisposable
{
    const byte k_FormFeed = 0x0C;
    const string k_ChannelName = "hubIPCService-test";

    readonly string m_AccessToken;
    readonly string m_DisplayName;
    readonly string m_SocketPath;

    Socket? m_ListenerSocket;
    CancellationTokenSource? m_Cts;
    Task? m_ServerTask;

    public MockHubIpcServer(string accessToken, string displayName)
    {
        m_AccessToken = accessToken;
        m_DisplayName = displayName;
        m_SocketPath = $"/tmp/Unity-{k_ChannelName}.sock";
    }

    public void Start()
    {
        m_Cts = new CancellationTokenSource();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            m_ServerTask = RunNamedPipeServerAsync(m_Cts.Token);
        }
        else
        {
            if (File.Exists(m_SocketPath))
                File.Delete(m_SocketPath);

            m_ListenerSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            m_ListenerSocket.Bind(new UnixDomainSocketEndPoint(m_SocketPath));
            m_ListenerSocket.Listen(1);
            m_ServerTask = RunUnixSocketServerAsync(m_Cts.Token);
        }
    }

    async Task RunUnixSocketServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await m_ListenerSocket!.AcceptAsync(cancellationToken);
                _ = HandleClientAsync(new NetworkStream(client, ownsSocket: true), cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    async Task RunNamedPipeServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var pipe = new NamedPipeServerStream(
                $"Unity-{k_ChannelName}", PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken);
                _ = HandleClientAsync(pipe, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await pipe.DisposeAsync();
                break;
            }
        }
    }

    async Task HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        await using (stream)
        {
            var buffer = new byte[4096];
            var accumulated = new List<byte>();

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                }
                catch { break; }

                if (bytesRead == 0) break;

                for (var i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == k_FormFeed)
                    {
                        var json = Encoding.UTF8.GetString(accumulated.ToArray());
                        accumulated.Clear();

                        var type = JsonDocument.Parse(json)
                            .RootElement.GetProperty("type").GetString()!;

                        var response = Encoding.UTF8.GetBytes(GetResponse(type));
                        await stream.WriteAsync(response, cancellationToken);
                        await stream.WriteAsync(new[] { k_FormFeed }, cancellationToken);
                        await stream.FlushAsync(cancellationToken);
                    }
                    else
                    {
                        accumulated.Add(buffer[i]);
                    }
                }
            }
        }
    }

    string GetResponse(string type) => type switch
    {
        "health:check" =>
            "{\"type\":\"health:check\",\"data\":{\"health\":true}}",
        "connectInfo:get" =>
            "{\"type\":\"connectInfo:changed\",\"data\":{\"initialized\":true,\"ready\":true,\"online\":true,\"loggedIn\":true,\"workOffline\":false,\"showLoginWindow\":false,\"error\":false,\"maintenance\":false}}",
        "userInfo:get" =>
            $"{{\"type\":\"userInfo:changed\",\"data\":{{\"valid\":true,\"accessToken\":\"{m_AccessToken}\",\"name\":\"testuser@unity3d.com\",\"displayName\":\"{m_DisplayName}\",\"userId\":\"test-uid\",\"primaryOrg\":\"test-org\",\"whitelisted\":false,\"organizationForeignKeys\":\"\"}}}}",
        "window:show" =>
            "{\"type\":\"window:show\",\"data\":{}}",
        _ =>
            "{\"type\":\"unknown\",\"data\":{}}",
    };

    public async ValueTask DisposeAsync()
    {
        if (m_Cts is not null)
        {
            await m_Cts.CancelAsync();

            if (m_ServerTask is not null)
            {
                try { await m_ServerTask; }
                catch (OperationCanceledException) { }
            }

            m_Cts.Dispose();
        }

        m_ListenerSocket?.Dispose();

        if (File.Exists(m_SocketPath))
            File.Delete(m_SocketPath);
    }
}
