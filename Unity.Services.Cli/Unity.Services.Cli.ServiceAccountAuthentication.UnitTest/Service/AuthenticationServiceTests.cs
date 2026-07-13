using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;

namespace Unity.Services.Cli.Authentication.UnitTest;

[TestFixture]
class AuthenticationServiceTests
{
    [Test]
    public async Task GetAccessTokenAsyncReturnsPersistedTokenIfAny()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        const string expectedToken = "test token";
        var persister = FakePersister(FakeLoad);
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, new NullHubAuthProvider());

        var token = await service.GetAccessTokenAsync();

        Assert.AreEqual(expectedToken, token);

        Task<string?> FakeLoad(CancellationToken _) => Task.FromResult<string?>(expectedToken);
    }

    [Test]
    public void GetAccessTokenAsyncThrowsIfNothingIsPersisted()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var persister = FakePersister(FakeLoad);
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, new NullHubAuthProvider());

        Assert.ThrowsAsync<MissingAccessTokenException>(() => service.GetAccessTokenAsync());

        Task<string?> FakeLoad(CancellationToken _) => Task.FromResult<string?>(null);
    }

    [Test]
    public async Task GetAccessTokenAsyncPrefersAuthTokenEnvVarOverPersistedToken()
    {
        // Embedding scenarios (e.g. orchestration tools that inject a fresh
        // bearer JWT) need an explicit env-var override to win over any
        // stale `ugs login` state on the developer's disk.
        const string staleServiceAccount = "stale-base64-keyid-secret";
        const string bearer = "eyJhbGciOiJSUzI1NiJ9.payload.sig";
        var sink = "";

        var mockEnvironmentProvider = new Mock<ISystemEnvironmentProvider>();
        mockEnvironmentProvider
            .Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out sink))
            .Returns(bearer);
        var persister = FakePersister(_ => Task.FromResult<string?>(staleServiceAccount));
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, new NullHubAuthProvider());

        var token = await service.GetAccessTokenAsync();

        Assert.AreEqual(AccessTokenHelper.BearerTokenSchemePrefix + bearer, token);
    }

    [Test]
    public async Task GetAccessTokenAsyncFallsBackToPersistedTokenWhenAuthTokenEnvIsBlank()
    {
        const string persistedToken = "persisted-service-account";
        var sink = "";

        var mockEnvironmentProvider = new Mock<ISystemEnvironmentProvider>();
        mockEnvironmentProvider
            .Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out sink))
            .Returns("   ");
        var persister = FakePersister(_ => Task.FromResult<string?>(persistedToken));
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, new NullHubAuthProvider());

        var token = await service.GetAccessTokenAsync();

        Assert.AreEqual(persistedToken, token);
    }

    static IPersister<string> FakePersister(Func<CancellationToken, Task<string?>> fakeLoad)
    {
        var mock = new Mock<IPersister<string>>();
        mock.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .Returns(fakeLoad);
        return mock.Object;
    }

    [Test]
    public async Task GetAccessTokenAsyncDelegatesToHubProviderWhenMarkerPersisted()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var mockHubProvider = new Mock<IHubAuthProvider>();
        mockHubProvider.Setup(h => h.IsHubAuthToken(AuthenticatorV1.HubAuthMarker)).Returns(true);
        mockHubProvider.Setup(h => h.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccessTokenHelper.BearerTokenSchemePrefix + "hub-jwt-token");

        var persister = FakePersister(_ => Task.FromResult<string?>(AuthenticatorV1.HubAuthMarker));
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, mockHubProvider.Object);

        var token = await service.GetAccessTokenAsync();

        Assert.AreEqual(AccessTokenHelper.BearerTokenSchemePrefix + "hub-jwt-token", token);
        mockHubProvider.Verify(h => h.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

#if FEATURE_HUB_AUTH
    [Test]
    public void GetAccessTokenAsyncThrowsWhenHubProviderThrows()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var mockHubProvider = new Mock<IHubAuthProvider>();
        mockHubProvider.Setup(h => h.IsHubAuthToken(AuthenticatorV1.HubAuthMarker)).Returns(true);
        mockHubProvider.Setup(h => h.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HubIpcUnavailableException(
                "Unity Hub is not running."));

        var persister = FakePersister(_ => Task.FromResult<string?>(AuthenticatorV1.HubAuthMarker));
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, mockHubProvider.Object);

        Assert.ThrowsAsync<HubIpcUnavailableException>(() => service.GetAccessTokenAsync());
    }
#endif

    [Test]
    public async Task GetAccessTokenAsyncIgnoresMarkerWhenBearerEnvVarSet()
    {
        var sink = "";
        const string bearer = "bearer-jwt";
        var mockEnvironmentProvider = new Mock<ISystemEnvironmentProvider>();
        mockEnvironmentProvider
            .Setup(s => s.GetSystemEnvironmentVariable(AuthenticatorV1.AuthToken, out sink))
            .Returns(bearer);

        var mockHubProvider = new Mock<IHubAuthProvider>();
        var persister = FakePersister(_ => Task.FromResult<string?>(AuthenticatorV1.HubAuthMarker));
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, mockHubProvider.Object);

        var token = await service.GetAccessTokenAsync();

        Assert.AreEqual(AccessTokenHelper.BearerTokenSchemePrefix + bearer, token);
        mockHubProvider.Verify(h => h.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAccessTokenAsyncUsesNormalTokenWhenNotMarker()
    {
        Mock<ISystemEnvironmentProvider> mockEnvironmentProvider = new();
        var mockHubProvider = new Mock<IHubAuthProvider>();
        const string normalToken = "normal-base64-token";

        var persister = FakePersister(_ => Task.FromResult<string?>(normalToken));
        var service = new AuthenticationService(persister, mockEnvironmentProvider.Object, mockHubProvider.Object);

        var token = await service.GetAccessTokenAsync();

        Assert.AreEqual(normalToken, token);
        mockHubProvider.Verify(h => h.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
