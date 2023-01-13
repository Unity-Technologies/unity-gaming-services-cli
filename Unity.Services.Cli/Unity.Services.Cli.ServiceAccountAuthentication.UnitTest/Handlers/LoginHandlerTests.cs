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
        var input = new LoginInput();

        await LoginHandler.LoginAsync(
            input, mockAuthenticator.Object, m_MockHelper.MockLogger.Object, CancellationToken.None);

        mockAuthenticator.Verify(a => a.LoginAsync(input, CancellationToken.None));
        TestsHelper.VerifyLoggerWasCalled(m_MockHelper.MockLogger, LogLevel.Information);
    }
}
