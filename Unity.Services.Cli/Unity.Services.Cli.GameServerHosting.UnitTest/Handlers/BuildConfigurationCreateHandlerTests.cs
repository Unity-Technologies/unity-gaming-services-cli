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
class BuildConfigurationCreateHandlerTests : HandlerCommon
{
    [Test]
    public async Task BuildConfigurationCreateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await BuildConfigurationCreateHandler.BuildConfigurationCreateAsync(
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
    public async Task BuildConfigurationCreateAsync_CallsFetchIdentifierAsync()
    {
        BuildConfigurationCreateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BinaryPath = "/test.exe",
            BuildId = 123,
            Configuration = new List<string>(),
            CommandLine = "--init test",
            Cores = 1,
            Memory = 100,
            Name = "test-build-config",
            QueryType = "none",
            Speed = 100,
            Readiness = true
        };

        await BuildConfigurationCreateHandler.BuildConfigurationCreateAsync(
            input,
            MockUnityEnvironment.Object,
            GameServerHostingService!,
            MockLogger!.Object,
            CancellationToken.None
        );

        MockUnityEnvironment.Verify(ex => ex.FetchIdentifierAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(
        null,
        ValidBuildConfigurationId,
        ValidBuildConfigurationCommandLine,
        ValidBuildConfigurationConfiguration,
        100,
        100,
        ValidBuildConfigurationName,
        ValidBuildConfigurationQueryType,
        100,
        true,
        TestName = "Missing BinaryPath")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        null,
        ValidBuildConfigurationCommandLine,
        ValidBuildConfigurationConfiguration,
        100,
        100,
        ValidBuildConfigurationName,
        ValidBuildConfigurationQueryType,
        100,
        true,
        TestName = "Missing BuildId")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        ValidBuildConfigurationId,
        null,
        ValidBuildConfigurationConfiguration,
        100,
        100,
        ValidBuildConfigurationName,
        ValidBuildConfigurationQueryType,
        100,
        true,
        TestName = "Missing CommandLine")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        ValidBuildConfigurationId,
        ValidBuildConfigurationCommandLine,
        null,
        100,
        100,
        ValidBuildConfigurationName,
        ValidBuildConfigurationQueryType,
        100,
        true,
        TestName = "Missing Configuration")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        ValidBuildConfigurationId,
        ValidBuildConfigurationCommandLine,
        ValidBuildConfigurationConfiguration,
        null,
        100,
        ValidBuildConfigurationName,
        ValidBuildConfigurationQueryType,
        100,
        true,
        TestName = "Missing Cores")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        ValidBuildConfigurationId,
        ValidBuildConfigurationCommandLine,
        ValidBuildConfigurationConfiguration,
        100,
        null,
        ValidBuildConfigurationName,
        ValidBuildConfigurationQueryType,
        100,
        true,
        TestName = "Missing Memory")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        ValidBuildConfigurationId,
        ValidBuildConfigurationCommandLine,
        ValidBuildConfigurationConfiguration,
        100,
        100,
        null,
        ValidBuildConfigurationQueryType,
        100,
        true,
        TestName = "Missing Name")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        ValidBuildConfigurationId,
        ValidBuildConfigurationCommandLine,
        ValidBuildConfigurationConfiguration,
        100,
        100,
        ValidBuildConfigurationName,
        null,
        100,
        true,
        TestName = "Missing QueryType")]
    [TestCase(
        ValidBuildConfigurationBinaryPath,
        ValidBuildConfigurationId,
        ValidBuildConfigurationCommandLine,
        ValidBuildConfigurationConfiguration,
        100,
        100,
        ValidBuildConfigurationName,
        ValidBuildConfigurationQueryType,
        null,
        true,
        TestName = "Missing Speed"),
        ]
    public Task BuildConfigurationCreateAsync_NullInputThrowsException(
        string? binaryPath,
        long? buildId,
        string? commandLine,
        string? configuration,
        long? cores,
        long? memory,
        string? name,
        string? queryType,
        long? speed,
        bool? readiness)
    {
        BuildConfigurationCreateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BinaryPath = binaryPath,
            BuildId = buildId,
            CommandLine = commandLine,
            Configuration = configuration == null
                ? null
                : new List<string>
                {
                    configuration
                },
            Cores = cores,
            Memory = memory,
            Name = name,
            QueryType = queryType,
            Speed = speed,
            Readiness = readiness
        };

        Assert.ThrowsAsync<MissingInputException>(
            () =>
                BuildConfigurationCreateHandler.BuildConfigurationCreateAsync(
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

    [TestCase("invalid", TestName = "No Separator")]
    [TestCase("key:value:value", TestName = "Too Many Separators")]
    [TestCase("", TestName = "Empty")]
    public Task BuildConfigurationCreateAsync_InvalidConfigurationInputThrowsException(string? configuration)
    {
        BuildConfigurationCreateInput input = new()
        {
            CloudProjectId = ValidProjectId,
            TargetEnvironmentName = ValidEnvironmentName,
            BinaryPath = ValidBuildConfigurationBinaryPath,
            BuildId = ValidBuildConfigurationBuildId,
            CommandLine = ValidBuildConfigurationCommandLine,
            Configuration = new List<string> { configuration! },
            Cores = 100,
            Memory = 100,
            Name = ValidBuildConfigurationName,
            QueryType = ValidBuildConfigurationQueryType,
            Speed = 100,
            Readiness = true
        };

        Assert.ThrowsAsync<InvalidKeyValuePairException>(
            () =>
                BuildConfigurationCreateHandler.BuildConfigurationCreateAsync(
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
}
