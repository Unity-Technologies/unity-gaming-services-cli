using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class BuildInstallsHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildInstallsAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildInstallsHandler.BuildInstallsAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildInstallsAsync_CallsFetchIdentifierAsync()
    {
        BuildIdInput input = new()
        {
            BuildId = ValidBuildId,
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName
        };

        await BuildInstallsHandler.BuildInstallsAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidBuildId,
        TestName = "ProjectId, EnvId and BuildId valid")]
    public async Task BuildInstallsAsync_CallsInstallsService(string projectId, string environmentName, string buildId)
    {
        BuildIdInput input = new()
        {
            BuildId = buildId,
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName
        };

        await BuildInstallsHandler.BuildInstallsAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        BuildsApi!.DefaultBuildsClient.Verify(api => api.GetBuildInstallsAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            long.Parse(input.BuildId), 0, CancellationToken.None
        ), Times.Once);
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId, InvalidBuildId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, ValidBuildId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, ValidBuildId)]
    [TestCase(ValidProjectId, ValidEnvironmentId, InvalidBuildId)]
    [TestCase(InvalidProjectId, InvalidEnvironmentId, ValidBuildId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, InvalidBuildId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, InvalidBuildId)]
    public void BuildInstallsAsync_InvalidInputThrowsException(string projectId, string environmentId, string buildId)
    {
        BuildIdInput input = new()
        {
            BuildId = buildId,
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentId
        };

        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            BuildInstallsHandler.BuildInstallsAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }
}
