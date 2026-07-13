#if FEATURE_HUB_AUTH
using System.CommandLine.Parsing;
using System.Net;
using System.Text;
using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub.TokenExchange;

namespace Unity.Services.Cli.Authentication.UnitTest;

[TestFixture]
class TokenExchangeClientTests
{
    [SetUp]
    public void Setup()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new TokenExchangeEndpoints()
        ]);
    }

    [Test]
    public async Task ExchangeAsyncPostsGenesisTokenAndReturnsUnityJwt()
    {
        const string genesisToken = "genesis-opaque-token";
        const string unityJwt = "eyJhbGciOiJSUzI1NiJ9.payload.sig";

        var handler = new StubHandler(request =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.That(request.RequestUri!.PathAndQuery,
                Is.EqualTo("/api/auth/v1/genesis-token-exchange/unity"));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"token\":\"{unityJwt}\"}}",
                    Encoding.UTF8,
                    "application/json"),
            };
        });

        var client = new TokenExchangeClient(new HttpClient(handler));
        var result = await client.ExchangeAsync(genesisToken, CancellationToken.None);

        Assert.AreEqual(unityJwt, result);
    }

    [Test]
    public async Task ExchangeAsyncSendsGenesisTokenInRequestBody()
    {
        const string genesisToken = "my-genesis-token";
        string? capturedBody = null;

        var handler = new StubHandler(async request =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"token\":\"jwt\"}",
                    Encoding.UTF8,
                    "application/json"),
            };
        });

        var client = new TokenExchangeClient(new HttpClient(handler));
        await client.ExchangeAsync(genesisToken, CancellationToken.None);

        Assert.That(capturedBody, Does.Contain("\"token\":\"my-genesis-token\""));
    }

    [Test]
    public void ExchangeAsyncThrowsOnHttpFailure()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var client = new TokenExchangeClient(new HttpClient(handler));

        Assert.ThrowsAsync<HubIpcUnavailableException>(
            () => client.ExchangeAsync("bad-token", CancellationToken.None));
    }

    [Test]
    public void ExchangeAsyncThrowsOnEmptyResponseToken()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"token\":\"\"}",
                    Encoding.UTF8,
                    "application/json"),
            });

        var client = new TokenExchangeClient(new HttpClient(handler));

        Assert.ThrowsAsync<HubIpcUnavailableException>(
            () => client.ExchangeAsync("genesis-token", CancellationToken.None));
    }

    [Test]
    public void ExchangeAsyncThrowsOnNetworkError()
    {
        var handler = new StubHandler(
            (Func<HttpRequestMessage, HttpResponseMessage>)(_ =>
                throw new HttpRequestException("Connection refused")));

        var client = new TokenExchangeClient(new HttpClient(handler));

        Assert.ThrowsAsync<HubIpcUnavailableException>(
            () => client.ExchangeAsync("genesis-token", CancellationToken.None));
    }

    class StubHandler : HttpMessageHandler
    {
        readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> m_Handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            m_Handler = req => Task.FromResult(handler(req));
        }

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            m_Handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => m_Handler(request);
    }
}
#endif
