#if FEATURE_HUB_AUTH
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

class HubIpcClient : IHubIpcClient
{
    const byte k_FormFeed = 0x0C;
    static readonly TimeSpan k_HealthCheckTimeout = TimeSpan.FromSeconds(3);
    static readonly TimeSpan k_PollInterval = TimeSpan.FromSeconds(2);

    readonly IHubIpcTransport m_Transport;
    readonly SemaphoreSlim m_Lock = new(1, 1);
    readonly MemoryStream m_ReceiveBuffer = new();
    bool m_Connected;

    public HubIpcClient(IHubIpcTransport transport)
    {
        m_Transport = transport;
    }

    public async Task<bool> TryConnectAsync(CancellationToken cancellationToken)
    {
        await m_Lock.WaitAsync(cancellationToken);
        try
        {
            if (m_Connected)
                return true;

            try
            {
                await m_Transport.ConnectAsync(cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(k_HealthCheckTimeout);

            try
            {
                await SendMessageAsync("health:check", "{}", timeoutCts.Token);
                var response = await ReadMessageAsync(timeoutCts.Token);

                if (response.Type == "health:check")
                {
                    var health = response.Data.Deserialize(HubIpcJsonContext.Default.HealthCheckData);
                    if (health?.Health == true)
                    {
                        m_Connected = true;
                        return true;
                    }
                }

                return false;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }
        finally
        {
            m_Lock.Release();
        }
    }

    public async Task<bool> IsLoggedInAsync(CancellationToken cancellationToken)
    {
        await m_Lock.WaitAsync(cancellationToken);
        try
        {
            await SendMessageAsync("connectInfo:get", "{}", cancellationToken);
            var response = await WaitForMessageTypeAsync("connectInfo:changed", cancellationToken);
            var connectInfo = response.Data.Deserialize(HubIpcJsonContext.Default.ConnectInfoData);
            return connectInfo?.LoggedIn == true;
        }
        finally
        {
            m_Lock.Release();
        }
    }

    public async Task<HubUserInfo> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        await m_Lock.WaitAsync(cancellationToken);
        try
        {
            await SendMessageAsync("userInfo:get", "{}", cancellationToken);
            var response = await WaitForMessageTypeAsync("userInfo:changed", cancellationToken);
            var userInfoData = response.Data.Deserialize(HubIpcJsonContext.Default.UserInfoData)
                ?? throw new HubIpcUnavailableException("Failed to parse user info from Unity Hub.");
            return HubUserInfo.FromData(userInfoData);
        }
        finally
        {
            m_Lock.Release();
        }
    }

    public async Task<HubUserInfo> WaitForLoginAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        await m_Lock.WaitAsync(timeoutCts.Token);
        try
        {
            var windowData = new WindowShowRequest
            {
                SessionId = Process.GetCurrentProcess().Id.ToString(),
                Url = "#/login",
                Modal = true,
            };
            await SendMessageAsync("window:show", JsonSerializer.Serialize(windowData, HubIpcJsonContext.Default.WindowShowRequest), timeoutCts.Token);
            // Read and discard the window:show response
            await WaitForMessageTypeAsync("window:show", timeoutCts.Token);
        }
        finally
        {
            m_Lock.Release();
        }

        try
        {
            while (!timeoutCts.Token.IsCancellationRequested)
            {
                await Task.Delay(k_PollInterval, timeoutCts.Token);

                await m_Lock.WaitAsync(timeoutCts.Token);
                try
                {
                    await SendMessageAsync("userInfo:get", "{}", timeoutCts.Token);
                    var response = await WaitForMessageTypeAsync("userInfo:changed", timeoutCts.Token);
                    var userInfoData = response.Data.Deserialize(HubIpcJsonContext.Default.UserInfoData);
                    if (userInfoData?.Valid == true && !string.IsNullOrEmpty(userInfoData.AccessToken))
                    {
                        return HubUserInfo.FromData(userInfoData);
                    }
                }
                finally
                {
                    m_Lock.Release();
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HubIpcUnavailableException(
                "Timed out waiting for Unity Hub login. Please sign in through Unity Hub and try again.");
        }

        throw new HubIpcUnavailableException(
            "Timed out waiting for Unity Hub login. Please sign in through Unity Hub and try again.");
    }

    internal async Task SendMessageAsync(string type, string dataJson, CancellationToken cancellationToken)
    {
        var json = $"{{\"type\":\"{type}\",\"data\":{dataJson}}}";
        var bytes = Encoding.UTF8.GetBytes(json);

        var stream = m_Transport.GetStream();
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.WriteAsync(new[] { k_FormFeed }, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    internal async Task<HubMessage> ReadMessageAsync(CancellationToken cancellationToken)
    {
        var bufferSpan = m_ReceiveBuffer.GetBuffer();
        var length = (int)m_ReceiveBuffer.Position;
        var ffIndex = Array.IndexOf(bufferSpan, k_FormFeed, 0, length);
        if (ffIndex >= 0)
        {
            return ExtractMessage(bufferSpan, ffIndex);
        }

        var stream = m_Transport.GetStream();
        var readBuffer = new byte[4096];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken);
            if (bytesRead == 0)
                throw new HubIpcUnavailableException("Unity Hub closed the connection.");

            m_ReceiveBuffer.Write(readBuffer, 0, bytesRead);

            bufferSpan = m_ReceiveBuffer.GetBuffer();
            length = (int)m_ReceiveBuffer.Position;
            ffIndex = Array.IndexOf(bufferSpan, k_FormFeed, 0, length);
            if (ffIndex >= 0)
            {
                return ExtractMessage(bufferSpan, ffIndex);
            }
        }
    }

    HubMessage ExtractMessage(byte[] buffer, int formFeedIndex)
    {
        var json = Encoding.UTF8.GetString(buffer, 0, formFeedIndex);
        var remaining = (int)m_ReceiveBuffer.Position - formFeedIndex - 1;

        m_ReceiveBuffer.SetLength(0);
        m_ReceiveBuffer.Position = 0;
        if (remaining > 0)
        {
            m_ReceiveBuffer.Write(buffer, formFeedIndex + 1, remaining);
        }

        return JsonSerializer.Deserialize(json, HubIpcJsonContext.Default.HubMessage)
            ?? throw new HubIpcUnavailableException("Failed to parse message from Unity Hub.");
    }

    async Task<HubMessage> WaitForMessageTypeAsync(string expectedType, CancellationToken cancellationToken)
    {
        while (true)
        {
            var message = await ReadMessageAsync(cancellationToken);
            if (message.Type == expectedType)
                return message;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await m_Transport.DisposeAsync();
        m_Lock.Dispose();
    }
}
#endif
