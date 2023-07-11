using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class ServerGetHandlerTests : HandlerCommon
{
    [Test]
    public async Task ServerGetAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();
        await ServerGetHandler.ServerGetAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(
                It.IsAny<string>(),
                It.IsAny<Func<StatusContext?, Task>>()
            ),
            Times.Once
        );
    }

    [Test]
    public async Task ServerGetAsync_CallsFetchIdentifierAsync()
    {
        ServerIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerId = ValidServerId.ToString()
        };

        await ServerGetHandler.ServerGetAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void ServerGetAsync_NullServerIdThrowsException()
    {
        ServerIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerId = null
        };

        Assert.ThrowsAsync<MissingInputException>(
            () =>
                ServerGetHandler.ServerGetAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        ServersApi!.DefaultServersClient.Verify(
            api => api.GetServerAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                0,
                0,
                CancellationToken.None
            ),
            Times.Never);

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidServerId)]
    public async Task ServerGetAsync_CallsGetService(string projectId, string environmentName, long serverId)
    {
        ServerIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            ServerId = serverId.ToString()
        };

        await ServerGetHandler.ServerGetAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        ServersApi!.DefaultServersClient.Verify(
            api => api.GetServerAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                0,
                0,
                CancellationToken.None
            ),
            Times.Never);
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId, InvalidServerId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, InvalidServerId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, InvalidServerId)]
    [TestCase(InvalidProjectId, InvalidEnvironmentId, ValidServerId)]
    public void ServerGetAsync_InvalidInputThrowsException(string projectId, string environmentId, long serverId)
    {
        ServerIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            ServerId = serverId.ToString()
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(
            () =>
                ServerGetHandler.ServerGetAsync(
                    input,
                    MockUnityEnvironment.Object,
                    GameServerHostingService!,
                    MockLogger!.Object,
                    CancellationToken.None
                )
        );

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            Times.Never);
    }
}
