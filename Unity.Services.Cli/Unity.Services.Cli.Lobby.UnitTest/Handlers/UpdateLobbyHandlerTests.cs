using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;
using Unity.Services.Cli.TestUtils;
using Unity.Services.MpsLobby.LobbyApiV1.Generated.Model;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers
{
    internal class UpdateLobbyHandlerTests
    {
        Mock<ILogger> m_MockLogger = new();
        Mock<ILobbyService> m_MockLobby = new();
        private Mock<IUnityEnvironment> m_MockUnityEnvironment = new();

        [SetUp]
        public void SetUp()
        {
            m_MockLobby = new();
            m_MockLobby.Setup(l =>
                    l.UpdateLobbyAsync(
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        CancellationToken.None))
                .Returns(Task.FromResult(new MpsLobby.LobbyApiV1.Generated.Model.Lobby()));
        }

        [Test]
        public void UpdateLobbyHandler_HandlesInputAndLogsOnSuccess()
        {
            var updateRequest = new UpdateRequest();
            var inputBody = JsonConvert.SerializeObject(updateRequest);
            var input = new RequiredBodyInput()
            {
                JsonFileOrBody = inputBody,
            };
            Assert.DoesNotThrowAsync(async () => await UpdateLobbyHandler.UpdateLobbyAsync(input, m_MockUnityEnvironment.Object, m_MockLobby.Object, m_MockLogger.Object, default));
            TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
        }

        [Test]
        public void UpdateLobbyHandler_MissingBodyThrowsException()
        {
            var input = new RequiredBodyInput();
            Assert.ThrowsAsync<CliException>(async () => await UpdateLobbyHandler.UpdateLobbyAsync(input, m_MockUnityEnvironment.Object, m_MockLobby.Object, m_MockLogger.Object, default));
        }
    }
}
