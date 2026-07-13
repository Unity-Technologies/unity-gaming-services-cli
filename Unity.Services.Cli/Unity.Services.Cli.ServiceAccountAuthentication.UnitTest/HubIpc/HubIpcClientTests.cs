#if FEATURE_HUB_AUTH
using System.Text;
using System.Text.Json;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

namespace Unity.Services.Cli.Authentication.UnitTest.HubIpc;

[TestFixture]
class HubIpcClientTests
{
    const byte k_FormFeed = 0x0C;
    Mock<IHubIpcTransport> m_MockTransport = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockTransport = new Mock<IHubIpcTransport>();
    }

    static byte[] MakeMessage(string type, object data)
    {
        var json = JsonSerializer.Serialize(new { type, data });
        var bytes = Encoding.UTF8.GetBytes(json);
        var result = new byte[bytes.Length + 1];
        bytes.CopyTo(result, 0);
        result[^1] = k_FormFeed;
        return result;
    }

    static byte[] ConcatMessages(params byte[][] messages)
    {
        var totalLength = messages.Sum(m => m.Length);
        var result = new byte[totalLength];
        var offset = 0;
        foreach (var msg in messages)
        {
            msg.CopyTo(result, offset);
            offset += msg.Length;
        }

        return result;
    }

    TestDuplexStream SetupTransportWithResponses(params byte[][] messages)
    {
        var readData = ConcatMessages(messages);
        var stream = new TestDuplexStream(readData);

        m_MockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m_MockTransport.Setup(t => t.GetStream()).Returns(stream);

        return stream;
    }

    [Test]
    public async Task TryConnectAsyncReturnsTrueWhenHealthCheckSucceeds()
    {
        var healthResponse = MakeMessage("health:check", new { health = true });
        SetupTransportWithResponses(healthResponse);

        await using var client = new HubIpcClient(m_MockTransport.Object);
        var result = await client.TryConnectAsync(CancellationToken.None);

        Assert.IsTrue(result);
    }

    [Test]
    public async Task TryConnectAsyncReturnsFalseWhenTransportThrows()
    {
        m_MockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Sockets.SocketException());

        await using var client = new HubIpcClient(m_MockTransport.Object);
        var result = await client.TryConnectAsync(CancellationToken.None);

        Assert.IsFalse(result);
    }

    [Test]
    public async Task TryConnectAsyncReturnsFalseWhenHealthCheckReturnsFalse()
    {
        var healthResponse = MakeMessage("health:check", new { health = false });
        SetupTransportWithResponses(healthResponse);

        await using var client = new HubIpcClient(m_MockTransport.Object);
        var result = await client.TryConnectAsync(CancellationToken.None);

        Assert.IsFalse(result);
    }

    [Test]
    public async Task IsLoggedInAsyncReturnsTrueWhenConnectInfoShowsLoggedIn()
    {
        var healthResponse = MakeMessage("health:check", new { health = true });
        var connectResponse = MakeMessage("connectInfo:changed", new
        {
            initialized = true,
            ready = true,
            online = true,
            loggedIn = true,
            workOffline = false,
            showLoginWindow = false,
            error = false,
            maintenance = false,
        });
        SetupTransportWithResponses(healthResponse, connectResponse);

        await using var client = new HubIpcClient(m_MockTransport.Object);
        await client.TryConnectAsync(CancellationToken.None);
        var result = await client.IsLoggedInAsync(CancellationToken.None);

        Assert.IsTrue(result);
    }

    [Test]
    public async Task IsLoggedInAsyncReturnsFalseWhenNotLoggedIn()
    {
        var healthResponse = MakeMessage("health:check", new { health = true });
        var connectResponse = MakeMessage("connectInfo:changed", new
        {
            initialized = true,
            ready = true,
            online = true,
            loggedIn = false,
            workOffline = false,
            showLoginWindow = true,
            error = false,
            maintenance = false,
        });
        SetupTransportWithResponses(healthResponse, connectResponse);

        await using var client = new HubIpcClient(m_MockTransport.Object);
        await client.TryConnectAsync(CancellationToken.None);
        var result = await client.IsLoggedInAsync(CancellationToken.None);

        Assert.IsFalse(result);
    }

    [Test]
    public async Task GetUserInfoAsyncReturnsValidUserInfo()
    {
        var healthResponse = MakeMessage("health:check", new { health = true });
        var userInfoResponse = MakeMessage("userInfo:changed", new
        {
            valid = true,
            whitelisted = true,
            userId = "user-123",
            accessToken = "jwt-token-here",
            accessTokenExpiration = 1718000000000L,
            name = "test@example.com",
            displayName = "Test User",
            primaryOrg = "org-1",
            organizationForeignKeys = "key1,key2",
        });
        SetupTransportWithResponses(healthResponse, userInfoResponse);

        await using var client = new HubIpcClient(m_MockTransport.Object);
        await client.TryConnectAsync(CancellationToken.None);
        var userInfo = await client.GetUserInfoAsync(CancellationToken.None);

        Assert.IsTrue(userInfo.Valid);
        Assert.AreEqual("jwt-token-here", userInfo.AccessToken);
        Assert.AreEqual("Test User", userInfo.DisplayName);
        Assert.AreEqual("user-123", userInfo.UserId);
        Assert.AreEqual("org-1", userInfo.PrimaryOrg);
        Assert.AreEqual(1718000000000L, userInfo.AccessTokenExpiration);
    }

    [Test]
    public async Task ReadMessageHandlesMultipleMessagesInSameChunk()
    {
        var msg1 = MakeMessage("health:check", new { health = true });
        var msg2 = MakeMessage("userInfo:changed", new
        {
            valid = true,
            whitelisted = true,
            userId = "u",
            accessToken = "tok",
            name = "n",
            displayName = "d",
            primaryOrg = "o",
            organizationForeignKeys = "",
        });
        SetupTransportWithResponses(msg1, msg2);

        await using var client = new HubIpcClient(m_MockTransport.Object);
        await client.TryConnectAsync(CancellationToken.None);
        var userInfo = await client.GetUserInfoAsync(CancellationToken.None);

        Assert.IsTrue(userInfo.Valid);
        Assert.AreEqual("tok", userInfo.AccessToken);
    }

    [Test]
    public async Task GetUserInfoAsyncThrowsWhenStreamCloses()
    {
        var healthResponse = MakeMessage("health:check", new { health = true });
        SetupTransportWithResponses(healthResponse);

        await using var client = new HubIpcClient(m_MockTransport.Object);
        await client.TryConnectAsync(CancellationToken.None);

        Assert.ThrowsAsync<HubIpcUnavailableException>(() => client.GetUserInfoAsync(CancellationToken.None));
    }
}
#endif
