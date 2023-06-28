using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers
{
    class HeartbeatLobbyHandlerTests
    {
        Mock<ILogger> m_MockLogger = new();
        Mock<ILobbyService> m_MockLobby = new();
        Mock<IUnityEnvironment> m_MockUnityEnvironment = new();

        [SetUp]
        public void SetUp()
        {
            m_MockLobby = new();
            m_MockLobby.Setup(l =>
                    l.HeartbeatLobbyAsync(
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string>(),
                        CancellationToken.None))
                .Returns(Task.CompletedTask);
        }

        [Test]
        public void HeartbeatLobbyHandler_HandlesInputAndLogsOnSuccess()
        {
            var input = new CommonLobbyInput();
            Assert.DoesNotThrowAsync(async () => await HeartbeatHandler.HeartbeatLobbyAsync(input, m_MockUnityEnvironment.Object, m_MockLobby.Object, m_MockLogger.Object, default));
            TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, null, Times.Once);
        }
    }
}
