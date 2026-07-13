using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Handlers;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Authentication.UnitTest.Handlers;

[TestFixture]
class LoginHandlerTests
{
    readonly MockHelper m_MockHelper = new();

    [SetUp]
    public void SetUp()
    {
        m_MockHelper.ClearInvocations();
    }

    [Test]
    public async Task LoginAsyncCallsRegisteredAuthenticatorLogin()
    {
        Mock<IAuthenticator> mockAuthenticator = new();
        mockAuthenticator.Setup(a => a.LoginAsync(It.IsAny<LoginInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginResult.ServiceAccount);
        var input = new LoginInput();

        await LoginHandler.LoginAsync(
            input, mockAuthenticator.Object, m_MockHelper.MockLogger.Object, CancellationToken.None);

        mockAuthenticator.Verify(a => a.LoginAsync(input, CancellationToken.None));
        TestsHelper.VerifyLoggerWasCalled(m_MockHelper.MockLogger, LogLevel.Information);
    }

    [Test]
    public async Task LoginAsyncLogsHubDisplayNameWhenResultHasDisplayName()
    {
        Mock<IAuthenticator> mockAuthenticator = new();
        mockAuthenticator.Setup(a => a.LoginAsync(It.IsAny<LoginInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResult("Test User"));
        var input = new LoginInput();

        await LoginHandler.LoginAsync(
            input, mockAuthenticator.Object, m_MockHelper.MockLogger.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_MockHelper.MockLogger, LogLevel.Information);
    }
}
