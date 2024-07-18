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
class BuildConfigurationUpdateHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildConfigurationUpdateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildConfigurationUpdateHandler.BuildConfigurationUpdateAsync(
            null!,
            MockUnityEnvironment.Object,
            null!,
            null!,
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex
                .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task BuildConfigurationUpdateAsync_CallsFetchIdentifierAsync()
    {
        BuildConfigurationUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildConfigId = ValidBuildConfigurationId,
        };

        await BuildConfigurationUpdateHandler.BuildConfigurationUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase("invalid", TestName = "No Separator")]
    [TestCase("key:value:value", TestName = "Too Many Separators")]
    [TestCase("", TestName = "Empty")]
    public Task BuildConfigurationUpdateAsync_InvalidConfigurationInputThrowsException(string? configuration)
    {
        BuildConfigurationUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildConfigurationBuildId,
            Configuration = new List<string>
            {
                configuration!
            },
        };

        Assert.ThrowsAsync<InvalidKeyValuePairException>(
            () =>
                BuildConfigurationUpdateHandler.BuildConfigurationUpdateAsync(
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
        return Task.CompletedTask;
    }

    [TestCase(
        10,
        null,
        10,
        TestName = "Cores empty")]
    [TestCase(
        10,
        10,
        null,
        TestName = "Memory empty")]
    [TestCase(
        null,
        10,
        10,
        TestName = "Speed empty")]
    public Task BuildConfigurationUpdateAsync_InvalidLegacyInputUsageException(long? speed, long? cores, long? memory)
    {
        BuildConfigurationUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildConfigurationBuildId,
            Configuration = new List<string>
            {
                "foo:bar"
            },
            Speed = speed,
            Cores = cores,
            Memory = memory,
        };

        Assert.ThrowsAsync<InvalidLegacyInputUsageException>(
            () =>
                BuildConfigurationUpdateHandler.BuildConfigurationUpdateAsync(
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
        return Task.CompletedTask;
    }

    [Test]
    public async Task BuildConfigurationUpdateAsync_DepricatetionWarning()
    {
        BuildConfigurationUpdateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BuildId = ValidBuildConfigurationBuildId,
            Configuration = new List<string>
            {
                "foo:bar"
            },
            Speed = 100,
            Memory = 100,
            Cores = 100,
        };

        await BuildConfigurationUpdateHandler.BuildConfigurationUpdateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        TestsHelper.VerifyLoggerWasCalled(
            MockLogger!,
            LogLevel.Warning,
            null,
            Times.Once);
    }
}
