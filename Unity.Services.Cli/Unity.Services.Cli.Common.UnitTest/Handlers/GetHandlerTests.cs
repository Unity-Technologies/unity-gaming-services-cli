using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Handlers;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Common.UnitTest.Handlers;

[TestFixture]
class GetHandlerTests
{
    [Test]
    public void GetConfigArgumentsAsyncThrowsMissingConfigExceptionWhenConfigAndEnvNotSet()
    {
        MockHelper mockHelper = new();
        ConfigurationInput input = new();
        input.Key = Keys.ConfigKeys.ProjectId;

        mockHelper.MockConfiguration
            .Setup(x => x.GetConfigArgumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(() => new MissingConfigurationException("key", "message"));

        Assert.ThrowsAsync<MissingConfigurationException>(() => mockHelper.MockConfiguration.Object.GetConfigArgumentsAsync(input.Key, CancellationToken.None));
    }

    [Test]
    public void GetConfigArgumentsAsyncLogsErrorWhenFailedToFetchFromConfigOrEnvironment()
    {
        MockHelper mockHelper = new();
        ConfigurationInput input = new();
        Mock<ISystemEnvironmentProvider> environmentProvider = new();
        input.Key = Keys.ConfigKeys.ProjectId;
        string? errorMsg;

        mockHelper.MockConfiguration
            .Setup(x => x.GetConfigArgumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(() => new MissingConfigurationException("project-id"));

        environmentProvider.Setup(ex => ex
            .GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg)).Returns((string?)null);

        Assert.ThrowsAsync<MissingConfigurationException>(() =>
                GetHandler.GetAsync(
                    input,
                    mockHelper.MockConfiguration.Object,
                    environmentProvider.Object,
                    mockHelper.MockLogger.Object,
                    CancellationToken.None)
        );

        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [Test]
    public void GetConfigArgumentsAsyncLogsErrorWhenFailedToFetchFromConfigAndEnvironmentKeyPairDoesNotExist()
    {
        MockHelper mockHelper = new();
        ConfigurationInput input = new();
        Mock<ISystemEnvironmentProvider> environmentProvider = new();
        input.Key = "test-key";

        mockHelper.MockConfiguration
            .Setup(x => x.GetConfigArgumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(() => new MissingConfigurationException(input.Key));

        Assert.ThrowsAsync<MissingConfigurationException>(() =>
            GetHandler.GetAsync(
                input,
                mockHelper.MockConfiguration.Object,
                environmentProvider.Object,
                mockHelper.MockLogger.Object,
                CancellationToken.None
                )
            );
        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Never);
    }

    [Test]
    public async Task GetConfigArgumentsAsyncCallsLoggerWhenFetchingFromConfigIsSuccessful()
    {
        MockHelper mockHelper = new();
        ConfigurationInput input = new();
        Mock<ISystemEnvironmentProvider> environmentProvider = new();
        input.Key = Keys.ConfigKeys.ProjectId;

        mockHelper.MockConfiguration
            .Setup(x => x.GetConfigArgumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult("test-value")!);

        await GetHandler.GetAsync(input, mockHelper.MockConfiguration.Object, environmentProvider.Object,
            mockHelper.MockLogger.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public async Task GetConfigArgumentsAsyncCallsLoggerWhenFetchingFromEnvironmentIsSuccessful()
    {
        MockHelper mockHelper = new();
        ConfigurationInput input = new();
        Mock<ISystemEnvironmentProvider> environmentProvider = new();
        string? errorMsg;
        input.Key = Keys.ConfigKeys.ProjectId;

        mockHelper.MockConfiguration
            .Setup(x => x.GetConfigArgumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(() => new MissingConfigurationException("key"));
        environmentProvider
            .Setup(ex => ex.GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg))
            .Returns("test-value");

        await GetHandler.GetAsync(input, mockHelper.MockConfiguration.Object, environmentProvider.Object,
            mockHelper.MockLogger.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public async Task GetAsyncCallsLogger()
    {
        MockHelper mockHelper = new();
        ConfigurationInput input = new();
        Mock<ISystemEnvironmentProvider> environmentProvider = new();

        await GetHandler.GetAsync(input, mockHelper.MockConfiguration.Object, environmentProvider.Object,
            mockHelper.MockLogger.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId);
    }

    [Test]
    public async Task GetAsyncCallsConfig()
    {
        MockHelper mockHelper = new();
        ConfigurationInput input = new();
        Mock<ISystemEnvironmentProvider> environmentProvider = new();
        input.Key = Keys.ConfigKeys.ProjectId;

        await GetHandler.GetAsync(input, mockHelper.MockConfiguration.Object, environmentProvider.Object,
            mockHelper.MockLogger.Object, CancellationToken.None);

        mockHelper.MockConfiguration.Verify(c => c.GetConfigArgumentsAsync(input.Key, CancellationToken.None));
    }
}
