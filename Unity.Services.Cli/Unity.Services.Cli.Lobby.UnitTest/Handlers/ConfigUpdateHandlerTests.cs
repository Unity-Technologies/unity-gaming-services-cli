using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers
{
    internal class ConfigUpdateHandlerTests
    {
        Mock<ILogger> m_MockLogger = new();
        Mock<IRemoteConfigService> m_MockRemoteConfig = new();

        [SetUp]
        public void SetUp()
        {
            m_MockRemoteConfig = new();
            m_MockRemoteConfig.Setup(l =>
                    l.UpdateConfigAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        CancellationToken.None))
                .Returns(Task.CompletedTask);
        }

        [Test]
        public void ConfigUpdateHandler_HandlesInputAndLogsOnSuccess()
        {
            var input = new LobbyConfigUpdateInput()
            {
                CloudProjectId = "projectid",
                JsonFileOrBody = "{}",
            };
            Assert.DoesNotThrowAsync(async () => await ConfigUpdateHandler.ConfigUpdateAsync(input, m_MockRemoteConfig.Object, m_MockLogger.Object, default));
            TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, null, Times.Once);
        }

        [Test]
        public void ConfigUpdateHandler_MissingProjectIdThrowsException()
        {
            var input = new LobbyConfigUpdateInput();
            Assert.ThrowsAsync<MissingConfigurationException>(async () => await ConfigUpdateHandler.ConfigUpdateAsync(input, m_MockRemoteConfig.Object, m_MockLogger.Object, default));
        }
    }
}
