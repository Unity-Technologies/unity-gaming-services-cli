using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Handlers;

namespace Unity.Services.Cli.Authentication.UnitTest.Handlers;

[TestFixture]
class StatusHandlerTests
{
    Mock<IAuthenticator>? m_MockAuthenticator;
    Mock<ISystemEnvironmentProvider>? m_EnvironmentProvider;
    Mock<ILogger>? m_MockedLogger;

    [SetUp]
    public void SetUp()
    {
        m_MockAuthenticator = new();
        m_EnvironmentProvider = new();
        m_MockedLogger = new();
    }

    [Test]
    public async Task GetStatusAsyncCallsRegisteredAuthenticatorGetToken()
    {
        Mock<IAuthenticator> mockAuthenticator = new();
        Mock<ISystemEnvironmentProvider> environmentProvider = new();
        var mockedLogger = new Mock<ILogger>();
        await StatusHandler.GetStatusAsync(mockAuthenticator.Object, environmentProvider.Object, mockedLogger.Object, CancellationToken.None);

        mockAuthenticator.Verify(a => a.GetTokenAsync(CancellationToken.None));
        mockedLogger.Verify(x => x.Log(
            It.Is<LogLevel>(level => level.Equals(LogLevel.Information)),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((o, t) => true)), Times.Exactly(1));
    }

    [Test]
    public async Task GetStatusAsync_ReturnsLoggedOutWhenNoAccessToken()
    {
        string errorMsg;
        m_EnvironmentProvider!.Setup(ex => ex.
                GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg)).Returns("");
        m_MockAuthenticator!.Setup(ex => ex
            .GetTokenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("")!);

        await StatusHandler.GetStatusAsync(m_MockAuthenticator.Object, m_EnvironmentProvider.Object,
            m_MockedLogger!.Object, CancellationToken.None);

        m_MockAuthenticator.Verify(a => a.GetTokenAsync(CancellationToken.None));

        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    string.Equals(StatusHandler.NoServiceAccountKeysMessage, o.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetStatusAsync_ReturnsLoggedInWhenAccessTokenOnlyFromLocalConfig()
    {
        string expectedLoggedMessage = "Using Service Account key from local configuration.";
        string errorMsg;
        m_EnvironmentProvider!.Setup(ex => ex.
            GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg)).Returns("");
        m_MockAuthenticator!.Setup(ex => ex
            .GetTokenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("1234")!);

        await StatusHandler.GetStatusAsync(m_MockAuthenticator.Object, m_EnvironmentProvider.Object,
            m_MockedLogger!.Object, CancellationToken.None);

        m_MockAuthenticator.Verify(a => a.GetTokenAsync(CancellationToken.None));

        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    string.Equals(expectedLoggedMessage, o.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetStatusAsync_ReturnsLoggedInThroughLocalConfigWhenLocalTokenSetAndEnvTokenSet()
    {
        string expectedLoggedMessage = "Using Service Account key from local configuration.";
        string errorMsg;
        m_EnvironmentProvider!.Setup(ex => ex.
            GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg)).Returns("1234");
        m_MockAuthenticator!.Setup(ex => ex
            .GetTokenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("1234")!);

        await StatusHandler.GetStatusAsync(m_MockAuthenticator.Object, m_EnvironmentProvider.Object,
            m_MockedLogger!.Object, CancellationToken.None);

        m_MockAuthenticator.Verify(a => a.GetTokenAsync(CancellationToken.None));

        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    string.Equals(expectedLoggedMessage, o.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetStatusAsync_ReturnsLoggedInThroughEnvVariablesWhenLocalTokenNotSetAndEnvTokenSet()
    {
        string expectedLoggedMessage = "Using Service Account key from system environment variables.";
        string errorMsg;
        m_EnvironmentProvider!.Setup(ex => ex.
            GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg)).Returns("1234");
        m_MockAuthenticator!.Setup(ex => ex
            .GetTokenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("")!);

        await StatusHandler.GetStatusAsync(m_MockAuthenticator.Object, m_EnvironmentProvider.Object,
            m_MockedLogger!.Object, CancellationToken.None);

        m_MockAuthenticator.Verify(a => a.GetTokenAsync(CancellationToken.None));

        m_MockedLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    string.Equals(expectedLoggedMessage, o.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
