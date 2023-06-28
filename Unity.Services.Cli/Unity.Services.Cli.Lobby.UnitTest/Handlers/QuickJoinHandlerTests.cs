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
    class QuickJoinHandlerTests
    {
        Mock<ILogger> m_MockLogger = new();
        Mock<ILobbyService> m_MockLobby = new();
        Mock<IUnityEnvironment> m_MockUnityEnvironment = new();

        [SetUp]
        public void SetUp()
        {
            m_MockLobby = new();
            m_MockLobby.Setup(l =>
                    l.QuickJoinAsync(
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        CancellationToken.None))
                .Returns(Task.FromResult(new MpsLobby.LobbyApiV1.Generated.Model.Lobby()));
        }

        [Test]
        public void QuickJoinHandler_HandlesInputAndLogsOnSuccess()
        {
            var queryFilter = new QueryFilter(QueryFilter.FieldEnum.AvailableSlots, "1", QueryFilter.OpEnum.GE);
            var queryFilterString = JsonConvert.SerializeObject(queryFilter);
            var playerDetails = new Player();
            var playerDetailsString = JsonConvert.SerializeObject(playerDetails);
            var input = new JoinInput()
            {
                QueryFilter = queryFilterString,
                PlayerDetails = playerDetailsString,
            };
            Assert.DoesNotThrowAsync(async () => await QuickJoinHandler.QuickJoinAsync(input, m_MockUnityEnvironment.Object, m_MockLobby.Object, m_MockLogger.Object, default));
            TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
        }

        [Test]
        public void QuickJoinLobbyHandler_MissingBodyThrowsException()
        {
            var input = new CommonLobbyInput();
            Assert.ThrowsAsync<CliException>(async () => await QuickJoinHandler.QuickJoinAsync(input, m_MockUnityEnvironment.Object, m_MockLobby.Object, m_MockLogger.Object, default));
        }
    }
}
