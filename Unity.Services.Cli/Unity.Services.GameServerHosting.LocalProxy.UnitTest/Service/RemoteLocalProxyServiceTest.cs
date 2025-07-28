using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Unity.Services.GameServerHosting.LocalProxy.Service;

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.Service;

[TestFixture]
class RemoteLocalProxyServiceTest
{
    const string k_ValidProjectId = "00000000-0000-0000-0000-000000000010";
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000020";
    const string k_ValidServerId = "1234";
    const string k_ValidAllocationId = "00000000-0000-0000-0000-000000000030";

    [Test]
    public async Task TestHandleFetchUnityJwtToken()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Created,
            Content = new StringContent("""{"token":"123456", "error": null}""")
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleFetchUnityJwtToken(memStream, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 201 Created", response);
        StringAssert.Contains("{\"token\":\"123456\", \"error\": null}", response);
    }

    [Test]
    public async Task TestHandleUpdateServerState()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NoContent,
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        const string rawRequest = $"PATCH v1/servers/{k_ValidServerId}/allocations/{k_ValidAllocationId} HTTP/1.1\r\n" +
                                  "Host: 127.0.0.1:8086\r\n" +
                                  "Accept: */*\r\n" +
                                  "{\"ready\":true}";

        using var memStream = new MemoryStream(100);
        await service.HandleUpdateServerState(memStream, rawRequest, k_ValidServerId, k_ValidAllocationId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 204 No Content", response);
    }

    [Test]
    public async Task TestHandleHoldServer()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""{"expiresAt":"123456", "held": true}""")
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        const string rawRequest = $"POST v1/servers/{k_ValidServerId}/hold HTTP/1.1\r\n" +
                                  "Host: 127.0.0.1:8086\r\n" +
                                  "Accept: */*\r\n" +
                                  "{\"timeout\":\"5m\"}";

        using var memStream = new MemoryStream(100);
        await service.HandleHoldServer(memStream, rawRequest, k_ValidServerId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 200 OK", response);
        StringAssert.Contains("{\"expiresAt\":\"123456\", \"held\": true}", response);
    }

    [Test]
    public async Task TestHandleServerHoldStatus()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""{"expiresAt":"123456", "held": true}""")
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleServerHoldStatus(memStream, k_ValidServerId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 200 OK", response);
        StringAssert.Contains("{\"expiresAt\":\"123456\", \"held\": true}", response);
    }

    [Test]
    public async Task TestHandleRemoveServerHold()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NoContent
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleRemoveServerHold(memStream, k_ValidServerId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 204 No Content", response);
    }

    [Test]
    public async Task TestHandleRetrieveAllocationPayload()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("ThisIsThePayloadString")
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleRetrieveAllocationPayload(memStream, k_ValidAllocationId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 200 OK", response);
        StringAssert.Contains("ThisIsThePayloadString", response);
    }

    [Test]
    public async Task TestHandleReserveServer()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("""{"buildConfigurationId":"123456", "gamePort": 8888, "ipv4": "192.168.0.1", "reservationId": "999"}""")
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleReserveServer(memStream, k_ValidServerId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 200 OK", response);
        StringAssert.Contains("{\"buildConfigurationId\":\"123456\", \"gamePort\": 8888, \"ipv4\": \"192.168.0.1\", \"reservationId\": \"999\"}", response);
    }

    [Test]
    public async Task TestHandleUnreserveServer()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NoContent
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleUnreserveServer(memStream, k_ValidServerId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 204 No Content", response);
    }

    [Test]
    public async Task TestHandleReadyForPlayers()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"status\":\"ready\"}")
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleReadyForPlayers(memStream, k_ValidServerId, k_ValidAllocationId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString() == $"https://localhost/v1/server/{k_ValidServerId}/allocation/{k_ValidAllocationId}/ready-for-players"
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 200 OK", response);
        StringAssert.Contains("{\"status\":\"ready\"}", response);
    }

    [Test]
    public async Task TestHandleUnreadyForPlayers()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"status\":\"unready\"}")
        };

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        await service.HandleUnreadyForPlayers(memStream, k_ValidServerId, CancellationToken.None);

        mockHttpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString() == $"https://localhost/v1/server/{k_ValidServerId}/unready"
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        var response = Encoding.UTF8.GetString(memStream.ToArray());
        StringAssert.Contains("HTTP/1.1 200 OK", response);
        StringAssert.Contains("{\"status\":\"unready\"}", response);
    }

    [Test]
    public void TestHandlesHttpRequestException()
    {
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var mockClient = new HttpClient(mockHttpHandler.Object);

        mockHttpHandler.Reset();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Throws(new HttpRequestException("test failure"));

        var service = new RemoteLocalProxyService(mockClient, k_ValidProjectId, k_ValidEnvironmentId)
        {
            GameServerHost = "localhost"
        };

        using var memStream = new MemoryStream(100);
        Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.HandleUnreserveServer(memStream, k_ValidServerId, CancellationToken.None));
    }
}
