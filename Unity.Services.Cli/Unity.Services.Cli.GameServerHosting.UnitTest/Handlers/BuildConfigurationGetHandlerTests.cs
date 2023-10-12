using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Model;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Handlers;

[TestFixture]
class BuildConfigurationGetHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildConfigurationGetAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildConfigurationGetHandler.BuildConfigurationGetAsync(null!, MockUnityEnvironment.Object, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task BuildConfigurationGetAsync_CallsFetchIdentifierAsync()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = ValidBuildConfigurationId.ToString()
        };

        await BuildConfigurationGetHandler.BuildConfigurationGetAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void BuildConfigurationGetAsync_NullBuildIdThrowsException()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = null
        };

        Assert.ThrowsAsync<MissingInputException>(() =>
            BuildConfigurationGetHandler.BuildConfigurationGetAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );

        BuildConfigurationsApi!.DefaultBuildConfigurationsClient.Verify(api => api.GetBuildConfigurationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<long>(), 0, CancellationToken.None
        ), Times.Never);

        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [Test]
    public async Task BuildConfigurationGetAsync_CallsGetService()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = ValidBuildConfigurationId.ToString()
        };
        await BuildConfigurationGetHandler.BuildConfigurationGetAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );
        BuildConfigurationsApi!.DefaultBuildConfigurationsClient.Verify(api => api.GetBuildConfigurationAsync(
            new Guid(ValidProjectId),
            new Guid(ValidEnvironmentId),
            ValidBuildConfigurationId,
            0,
            CancellationToken.None
        ), Times.Once);
    }

    [Test]
    public void BuildConfigurationGetAsync_InvalidInputThrowsException()
    {
        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = "invalid"
        };
        Assert.ThrowsAsync<FormatException>(() =>
            BuildConfigurationGetHandler.BuildConfigurationGetAsync(
                input,
                MockUnityEnvironment.Object,
                GameServerHostingService!,
                MockLogger!.Object,
                CancellationToken.None
            )
        );
        TestsHelper.VerifyLoggerWasCalled(MockLogger!, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [Test]
    public async Task BuildConfigurationGetAsync_ValidateLoggingOutput()
    {

        var buildConfiguration = new BuildConfiguration(
            binaryPath: "/path/to/simple-go-server",
            buildID: long.Parse(ValidBuildId),
            buildName: ValidBuildName,
            commandLine: "simple-go-server",
            configuration: new List<ConfigEntry>()
            {
                new(
                    id: 0,
                    key: "key",
                    value: "value"
                ),
            },
            cores: 2L,
            createdAt: new DateTime(2022, 10, 11),
            fleetID: new Guid(ValidFleetId),
            fleetName: ValidFleetName,
            id: ValidBuildConfigurationId,
            memory: 800L,
            name: ValidBuildConfigurationName,
            queryType: "sqp",
            speed: 1200L,
            updatedAt: new DateTime(2022, 10, 11),
            version: 1L
        );

        BuildConfigurationIdInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigurationId = ValidBuildConfigurationId.ToString()
        };

        await BuildConfigurationGetHandler.BuildConfigurationGetAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        TestsHelper.VerifyLoggerWasCalled(MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once, new BuildConfigurationOutput(buildConfiguration).ToString());
    }
}
