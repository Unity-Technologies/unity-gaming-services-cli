using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers
{
    class ConfigGetHandlerTests
    {
        Mock<ILogger> m_MockLogger = new();
        Mock<IRemoteConfigService> m_MockRemoteConfig = new();
        Mock<IUnityEnvironment> m_MockUnityEnvironment = new();

        [SetUp]
        public void SetUp()
        {
            m_MockRemoteConfig = new();
            m_MockRemoteConfig.Setup(l =>
                    l.GetAllConfigsFromEnvironmentAsync(
                        It.IsAny<string>(),
                        It.IsAny<string?>(),
                        It.IsAny<string?>(),
                        CancellationToken.None))
                .Returns(Task.FromResult(string.Empty));
        }

        [Test]
        public void ConfigGetHandler_HandlesInputAndLogsOnSuccess()
        {
            var input = new CommonLobbyInput()
            {
                CloudProjectId = "projectid"
            };
            Assert.DoesNotThrowAsync(async () => await ConfigGetHandler.ConfigGetAsync(input, m_MockUnityEnvironment.Object, m_MockRemoteConfig.Object, m_MockLogger.Object, default));
            TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
        }

        [Test]
        public void ConfigGetHandler_MissingProjectIdThrowsException()
        {
            var input = new CommonLobbyInput();
            Assert.ThrowsAsync<MissingConfigurationException>(async () => await ConfigGetHandler.ConfigGetAsync(input, m_MockUnityEnvironment.Object, m_MockRemoteConfig.Object, m_MockLogger.Object, default));
        }
    }
}
