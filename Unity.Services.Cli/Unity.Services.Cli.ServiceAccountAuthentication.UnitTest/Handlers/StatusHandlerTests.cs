using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Handlers;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub;

namespace Unity.Services.Cli.Authentication.UnitTest.Handlers;

[TestFixture]
class StatusHandlerTests
{
    Mock<IAuthenticator> m_MockAuthenticator = null!;
    Mock<ISystemEnvironmentProvider> m_EnvironmentProvider = null!;
    Mock<IHubAuthProvider> m_MockHubAuthProvider = null!;
    Mock<ILogger> m_MockedLogger = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockAuthenticator = new();
        m_EnvironmentProvider = new();
        m_MockHubAuthProvider = new();
        m_MockedLogger = new();

        // Default: every env var returns null. Tests override the ones they
        // need so each precedence path is exercised in isolation.
        var sink = "";
        m_EnvironmentProvider
            .Setup(ex => ex.GetSystemEnvironmentVariable(It.IsAny<string>(), out sink))
            .Returns((string?)null);

        // Default: not hub auth.
        m_MockHubAuthProvider
            .Setup(h => h.IsHubAuthToken(It.IsAny<string?>()))
            .Returns(false);
    }

    void StubEnv(string name, string? value)
    {
        var sink = "";
        m_EnvironmentProvider
            .Setup(ex => ex.GetSystemEnvironmentVariable(name, out sink))
            .Returns(value);
    }

    void StubPersistedToken(string? token)
    {
        m_MockAuthenticator
            .Setup(ex => ex.GetTokenAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(token));
    }

    void StubHubAuth()
    {
        m_MockHubAuthProvider
            .Setup(h => h.IsHubAuthToken(AuthenticatorV1.HubAuthMarker))
            .Returns(true);
    }

    Task RunStatus() =>
        StatusHandler.GetStatusAsync(
            m_MockAuthenticator.Object,
            m_EnvironmentProvider.Object,
            m_MockHubAuthProvider.Object,
            m_MockedLogger.Object,
            CancellationToken.None);

    void VerifyLogged(LogLevel level, string expectedMessage, Times times) =>
        m_MockedLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(expectedMessage, o.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    void VerifyNoLogAtLevel(LogLevel level) =>
        m_MockedLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

    [Test]
    public async Task ReportsNoCredentialsWhenNothingConfigured()
    {
        await RunStatus();

        m_MockAuthenticator.Verify(a => a.GetTokenAsync(CancellationToken.None));
        VerifyLogged(LogLevel.Information, StatusHandler.NoCredentialsMessage, Times.Once());
        VerifyNoLogAtLevel(LogLevel.Warning);
    }

    [Test]
    public async Task ReportsAuthTokenWhenOnlyBearerEnvSet()
    {
        StubEnv(AuthenticatorV1.AuthToken, "eyJhbGciOiJSUzI1NiJ9.payload.sig");

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingAuthTokenMessage, Times.Once());
        VerifyNoLogAtLevel(LogLevel.Warning);
    }

    [Test]
    public async Task ReportsLoginConfigWhenOnlyPersistedTokenSet()
    {
        StubPersistedToken("base64-keyid-secret");

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingLoginConfigMessage, Times.Once());
        VerifyNoLogAtLevel(LogLevel.Warning);
    }

    [Test]
    public async Task ReportsServiceKeyEnvWhenOnlyServiceKeyEnvSet()
    {
        StubEnv(AuthenticatorV1.ServiceKeyId, "key-id");
        StubEnv(AuthenticatorV1.ServiceSecretKey, "secret-key");

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingServiceKeyEnvMessage, Times.Once());
        VerifyNoLogAtLevel(LogLevel.Warning);
    }

    [Test]
    public async Task AuthTokenWinsAndWarnsAboutOverriddenLoginAndServiceKeyEnv()
    {
        StubEnv(AuthenticatorV1.AuthToken, "eyJhbGciOiJSUzI1NiJ9.payload.sig");
        StubPersistedToken("base64-keyid-secret");
        StubEnv(AuthenticatorV1.ServiceKeyId, "key-id");
        StubEnv(AuthenticatorV1.ServiceSecretKey, "secret-key");

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingAuthTokenMessage, Times.Once());
        // Exactly one warning that names both overridden sources.
        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    o.ToString()!.Contains("saved login")
                    && o.ToString()!.Contains(AuthenticatorV1.ServiceKeyId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task LoginConfigWinsAndWarnsAboutOverriddenServiceKeyEnv()
    {
        StubPersistedToken("base64-keyid-secret");
        StubEnv(AuthenticatorV1.ServiceKeyId, "key-id");
        StubEnv(AuthenticatorV1.ServiceSecretKey, "secret-key");

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingLoginConfigMessage, Times.Once());
        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(AuthenticatorV1.ServiceKeyId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ReportsHubAuthWhenMarkerPersisted()
    {
        StubHubAuth();
        StubPersistedToken(AuthenticatorV1.HubAuthMarker);

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingHubAuthMessage, Times.Once());
        VerifyNoLogAtLevel(LogLevel.Warning);
    }

    [Test]
    public async Task HubAuthOverriddenByBearerTokenReportsWarning()
    {
        StubHubAuth();
        StubEnv(AuthenticatorV1.AuthToken, "eyJhbGciOiJSUzI1NiJ9.payload.sig");
        StubPersistedToken(AuthenticatorV1.HubAuthMarker);

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingAuthTokenMessage, Times.Once());
        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Unity Hub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task HubAuthWinsOverServiceKeyEnvAndWarns()
    {
        StubHubAuth();
        StubPersistedToken(AuthenticatorV1.HubAuthMarker);
        StubEnv(AuthenticatorV1.ServiceKeyId, "key-id");
        StubEnv(AuthenticatorV1.ServiceSecretKey, "secret-key");

        await RunStatus();

        VerifyLogged(LogLevel.Information, StatusHandler.UsingHubAuthMessage, Times.Once());
        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(AuthenticatorV1.ServiceKeyId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
