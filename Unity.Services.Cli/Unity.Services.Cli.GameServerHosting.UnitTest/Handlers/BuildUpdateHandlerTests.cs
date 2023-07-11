using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class BuildUpdateHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildUpdateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildUpdateHandler.BuildUpdateAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildUpdateAsync_CallsFetchIdentifierAsync()
    {
        BuildUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildIdContainer.ToString(),
            BuildName = ValidBuildName
        };

        await BuildUpdateHandler.BuildUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, null, ValidBuildName)]
    public void BuildUpdateAsync_NullBuildIdThrowsException(
        string projectId,
        string environmentName,
        string buildId,
        string newBuildName
    )
    {
        BuildUpdateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            BuildId = buildId
        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            BuildUpdateHandler.BuildUpdateAsync(
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

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidBuildIdContainer, ValidBuildName)]
    public async Task BuildUpdateAsync_CallsUpdateService(
        string projectId,
        string environmentName,
        long buildId,
        string newBuildName
    )
    {
        BuildUpdateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName,
            BuildId = buildId.ToString(),
            BuildName = newBuildName
        };

        await BuildUpdateHandler.BuildUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        BuildsApi!.DefaultBuildsClient.Verify(api => api.UpdateBuildAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            long.Parse(input.BuildId), new UpdateBuildRequest(input.BuildName), 0, CancellationToken.None
        ), Times.Once);

        // Clear invocations to Mock Environment
        MockUnityEnvironment.Invocations.Clear();
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId, InvalidBuildId, ValidBuildName)]
    [TestCase(ValidProjectId, InvalidEnvironmentId, InvalidBuildId, ValidBuildName)]
    [TestCase(InvalidProjectId, ValidEnvironmentId, InvalidBuildId, ValidBuildName)]
    [TestCase(InvalidProjectId, InvalidEnvironmentId, ValidBuildId, ValidBuildName)]
    public void BuildUpdateAsync_InvalidInputThrowsException(
        string projectId,
        string environmentId,
        string buildId,
        string newBuildName
    )
    {
        BuildUpdateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = buildId,
            BuildName = newBuildName
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            BuildUpdateHandler.BuildUpdateAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName, ValidBuildId, null)]
    public void BuildUpdateAsync_NullBuildNameThrowsException(
        string projectId,
        string environmentId,
        string buildId,
        string newBuildName
    )
    {
        BuildUpdateInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = buildId,
            BuildName = newBuildName
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<MissingInputException>(() =>
            BuildUpdateHandler.BuildUpdateAsync(
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
