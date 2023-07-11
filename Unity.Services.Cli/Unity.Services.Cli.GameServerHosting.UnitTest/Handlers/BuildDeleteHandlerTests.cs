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
class BuildDeleteHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildDeleteAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildDeleteHandler.BuildDeleteAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildDeleteAsync_CallsFetchIdentifierAsync()
    {
        BuildIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdContainer.ToString()
        };

        await BuildDeleteHandler.BuildDeleteAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, null)]
    public void BuildDeleteAsync_NullBuildIdThrowsException(string projectId, string environmentName, string buildId)
    {
        BuildIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            BuildId = buildId
        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            BuildDeleteHandler.BuildDeleteAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        BuildsApi!.DefaultBuildsClient.Verify(api => api.GetBuildAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<long>(), 0, CancellationToken.None
        ), Times.Never);

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [Test]
    public async Task BuildDeleteAsync_CallsDeleteService()
    {
        BuildIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdBucket.ToString()
        };

        await BuildDeleteHandler.BuildDeleteAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        BuildsApi!.DefaultBuildsClient.Verify(api => api.DeleteBuildAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            long.Parse(input.BuildId), null, 0, CancellationToken.None
        ), Times.Once);

        // Clear invocations to Mock Environment
        MockUnityEnvironment.Invocations.Clear();
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId, InvalidBuildId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, InvalidBuildId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, InvalidBuildId)]
    [TestCase(InvalidProjectId, InvalidEnvironmentId, ValidBuildId)]
    public void BuildDeleteAsync_InvalidInputThrowsException(string projectId, string environmentId, string buildId)
    {
        BuildIdInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = buildId
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            BuildDeleteHandler.BuildDeleteAsync(
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
