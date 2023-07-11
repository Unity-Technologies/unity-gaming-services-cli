using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class BuildListHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildListAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildListHandler.BuildListAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildListAsync_CallsFetchIdentifierAsync()
    {
        CommonInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName
        };

        await BuildListHandler.BuildListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(ValidProjectId, ValidEnvironmentName)]
    public async Task BuildListAsync_CallsListService(string projectId, string environmentName)
    {
        CommonInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = environmentName
        };

        await BuildListHandler.BuildListAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        BuildsApi!.DefaultBuildsClient.Verify(api => api.ListBuildsAsync(
            new Guid(input.CloudProjectId), new Guid(ValidEnvironmentId),
            null, null, null, null, null, null, 0, CancellationToken.None
        ), Times.Once);
    }

    [TestCase(InvalidProjectId, InvalidEnvironmentId)]
    [TestCase(ValidProjectId, InvalidEnvironmentId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId)]
    [TestCase(InvalidProjectId, ValidEnvironmentId)]
    public void BuildListAsync_InvalidInputThrowsException(string projectId, string environmentId)
    {
        CommonInput input = new()
        {
            CloudProjectId = projectId,
            TargetEnvironmentName = ValidEnvironmentName
        };
        MockUnityEnvironment.Setup(ex => ex.FetchIdentifierAsync(CancellationToken.None)).ReturnsAsync(environmentId);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            BuildListHandler.BuildListAsync(
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
