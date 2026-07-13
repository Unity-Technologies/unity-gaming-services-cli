using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.IntegrationTest.Common;
using Unity.Services.Cli.MockServer.Common;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Unity.Services.Cli.IntegrationTest.AuthTests;

public class HubLoginTests : UgsCliFixture
{
    const string k_GenesisToken = "genesis-test-token";
    const string k_ExchangedJwt = "eyJhbGciOiJSUzI1NiJ9.test-payload.test-sig";
    const string k_DisplayName = "Test User";
    const string k_HubAuthMarker = "__HUB_AUTH__";

    MockHubIpcServer? m_HubServer;

    [SetUp]
    public void Setup()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        MockApi.Server?.ResetMappings();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (m_HubServer is not null)
        {
            await m_HubServer.DisposeAsync();
            m_HubServer = null;
        }
    }

    void StartMockHub()
    {
        m_HubServer = new MockHubIpcServer(k_GenesisToken, k_DisplayName);
        m_HubServer.Start();
    }

    void MockTokenExchange()
    {
        MockApi.Server!.Given(
                Request.Create()
                    .WithPath("/api/auth/v1/genesis-token-exchange/unity")
                    .UsingPost())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody($"{{\"token\":\"{k_ExchangedJwt}\"}}"));
    }

    [Test]
    public async Task LoginViaHubSucceeds()
    {
        StartMockHub();
        MockTokenExchange();

        await NewUgsCliTestCase()
            .Command("login --unity-hub")
            .WaitForExit(AssertHubAuthMarkerPersisted)
            .AssertStandardErrorContains($"Logged in via Unity Hub as {k_DisplayName}.")
            .ExecuteAsync();
    }

    [Test]
    public async Task StatusShowsHubAuthAfterHubLogin()
    {
        StartMockHub();
        MockTokenExchange();

        await NewUgsCliTestCase()
            .Command("login --unity-hub")
            .Command("status")
            .AssertStandardErrorContains("Using Unity Hub authentication.")
            .ExecuteAsync();
    }

    [Test]
    public async Task LoginFallsBackToServiceAccountWhenHubUnavailable()
    {
        await GetLoggedInCli()
            .WaitForExit(AssertServiceAccountTokenPersisted)
            .ExecuteAsync();
    }

    void AssertHubAuthMarkerPersisted()
    {
        var content = File.ReadAllText(CredentialsFile);
        var savedToken = JsonConvert.DeserializeObject<string>(content);
        Assert.AreEqual(k_HubAuthMarker, savedToken);
    }

    void AssertServiceAccountTokenPersisted()
    {
        var content = File.ReadAllText(CredentialsFile);
        var savedToken = JsonConvert.DeserializeObject<string>(content);
        Assert.AreEqual(CommonKeys.ValidAccessToken, savedToken);
    }
}
