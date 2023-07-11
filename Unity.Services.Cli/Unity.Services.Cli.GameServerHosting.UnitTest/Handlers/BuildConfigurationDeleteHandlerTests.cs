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
class BuildConfigurationDeleteHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildConfigurationDeleteAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildConfigurationDeleteHandler.BuildConfigurationDeleteAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildConfigurationDeleteAsync_CallsFetchIdentifierAsync()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = ValidBuildConfigurationId.ToString()
        };

        await BuildConfigurationDeleteHandler.BuildConfigurationDeleteAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void BuildConfigurationDeleteAsync_NullBuildIdThrowsException()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = null
        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            BuildConfigurationDeleteHandler.BuildConfigurationDeleteAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        BuildConfigurationsApi!.DefaultBuildConfigurationsClient.Verify(api => api.DeleteBuildConfigurationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<long>(), null, 0, CancellationToken.None
        ), Times.Never);

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [Test]
    public async Task BuildConfigurationDeleteAsync_CallsDeleteService()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = ValidBuildConfigurationId.ToString()
        };
        await BuildConfigurationDeleteHandler.BuildConfigurationDeleteAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );
        BuildConfigurationsApi!.DefaultBuildConfigurationsClient.Verify(api => api.DeleteBuildConfigurationAsync(
            new Guid(ValidProjectId), new Guid(ValidEnvironmentId),
            ValidBuildConfigurationId, null, 0, CancellationToken.None
        ), Times.Once);
    }

    [Test]
    public void BuildConfigurationDeleteAsync_InvalidInputThrowsException()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = "invalid"
        };
        Assert.ThrowsAsync<FormatException>(() =>
            BuildConfigurationDeleteHandler.BuildConfigurationDeleteAsync(
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
