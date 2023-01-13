using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;
using Unity.Services.Cli.TestUtils;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Model;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers
{
    internal class RequestTokenHandlerTests
    {
        Mock<ILogger> m_MockLogger = new();
        Mock<ILobbyService> m_MockLobby = new();
        private Mock<IUnityEnvironment> m_MockUnityEnvironment = new();

        [SetUp]
        public void SetUp()
        {
            m_MockLobby = new();
            m_MockLobby.Setup(l =>
                    l.RequestTokenAsync(
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string>(),
                        It.IsAny<TokenRequest.TokenTypeEnum>(),
                        CancellationToken.None))
                .Returns(Task.FromResult(new Dictionary<string, TokenData>() as IDictionary<string, TokenData>));
        }

        [Test]
        public void RequestTokenHandler_HandlesInputAndLogsOnSuccess()
        {
            var input = new LobbyTokenInput();
            Assert.DoesNotThrowAsync(async () => await RequestTokenHandler.RequestTokenAsync(input, m_MockUnityEnvironment.Object, m_MockLobby.Object, m_MockLogger.Object, default));
            TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
        }
    }
}
