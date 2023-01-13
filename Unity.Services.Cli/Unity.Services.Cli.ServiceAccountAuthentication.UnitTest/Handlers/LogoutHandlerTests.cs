using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Handlers;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Authentication.UnitTest.Handlers;

[TestFixture]
class LogoutHandlerTests
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
    public async Task LogoutAsyncCallsRegisteredAuthenticatorLogout()
    {
        var mockedLogger = new Mock<ILogger>();

        m_MockAuthenticator!.Setup(ex => ex
                .LogoutAsync(m_EnvironmentProvider!.Object, CancellationToken.None))
            .Returns(Task.FromResult(new LogoutResponse("", "")));

        await LogoutHandler.LogoutAsync(m_MockAuthenticator.Object, m_EnvironmentProvider!.Object,
            mockedLogger.Object, CancellationToken.None);

        m_MockAuthenticator.Verify(a => a
            .LogoutAsync(m_EnvironmentProvider.Object, CancellationToken.None));
        TestsHelper.VerifyLoggerWasCalled(mockedLogger, LogLevel.Information);
    }

    [Test]
    public async Task LogoutAsync_OnlyLogsInformationWhenEnvironmentNotSet()
    {
        m_MockAuthenticator!.Setup(ex => ex
                .LogoutAsync(m_EnvironmentProvider!.Object, CancellationToken.None))
            .Returns(Task.FromResult(new LogoutResponse("", null)));

        await LogoutHandler.LogoutAsync(m_MockAuthenticator.Object, m_EnvironmentProvider!.Object,
            m_MockedLogger!.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_MockedLogger, LogLevel.Information, null, Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockedLogger, LogLevel.Warning, null, Times.Never);
    }

    [Test]
    public async Task LogoutAsync_LogsInformationAndWarningWhenEnvironmentSet()
    {
        m_MockAuthenticator!.Setup(ex => ex
            .LogoutAsync(m_EnvironmentProvider!.Object, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new LogoutResponse("info", "warning")));

        await LogoutHandler.LogoutAsync(m_MockAuthenticator.Object, m_EnvironmentProvider!.Object,
            m_MockedLogger!.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_MockedLogger, LogLevel.Information, null, Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockedLogger, LogLevel.Warning, null, Times.Once);
    }
}
