using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Handlers;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Common.UnitTest.Handlers;

[TestFixture]
class DeleteHandlerTests
{
    MockHelper mockHelper = new();
    Mock<ISystemEnvironmentProvider> m_MockSystemEnvironmentProvider = new();

    [SetUp]
    public void Setup()
    {
        mockHelper.MockLogger.Reset();
        mockHelper.MockConfiguration.Reset();
        m_MockSystemEnvironmentProvider.Reset();
    }

    [Test]
    public void DeleteAsync_NotHavingForceOptionThrows()
    {
        ConfigurationInput input = new ConfigurationInput
        {
            TargetAllKeys = true,
            UseForce = false
        };

        Assert.ThrowsAsync<CliException>(() => DeleteHandler.DeleteAsync(input, mockHelper.MockConfiguration.Object,
            mockHelper.MockLogger.Object, m_MockSystemEnvironmentProvider.Object, CancellationToken.None));

        mockHelper.MockConfiguration.Verify(ex => ex
            .DeleteConfigArgumentsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_HavingBothKeysAndAllKeysOptionSpecifiedThrows()
    {
        ConfigurationInput input = new ConfigurationInput
        {
            TargetAllKeys = true,
            Keys = new []{""},
            UseForce = true
        };

        Assert.ThrowsAsync<CliException>(() => DeleteHandler.DeleteAsync(input, mockHelper.MockConfiguration.Object,
            mockHelper.MockLogger.Object, m_MockSystemEnvironmentProvider.Object, CancellationToken.None));

        mockHelper.MockConfiguration.Verify(ex => ex
            .DeleteConfigArgumentsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void DeleteAsync_HavingNoOptionSpecifiedThrows()
    {
        Assert.ThrowsAsync<CliException>(() => DeleteHandler.DeleteAsync(new ConfigurationInput(),
            mockHelper.MockConfiguration.Object, mockHelper.MockLogger.Object, m_MockSystemEnvironmentProvider.Object,
            CancellationToken.None));

        mockHelper.MockConfiguration.Verify(ex => ex
            .DeleteConfigArgumentsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_CallsConfigurationAndLogsAllKeysDeleted()
    {
        ConfigurationInput input = new ConfigurationInput
        {
            TargetAllKeys = true,
            UseForce = true
        };

        await DeleteHandler.DeleteAsync(input, mockHelper.MockConfiguration.Object,
            mockHelper.MockLogger.Object, m_MockSystemEnvironmentProvider.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Information, null, null,
            DeleteHandler.k_DeletedAllKeysMsg);

        mockHelper.MockConfiguration.Verify(ex => ex
            .DeleteConfigArgumentsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CallsConfigurationAndLogsSpecifiedKeysDeleted()
    {
        ConfigurationInput input = new ConfigurationInput
        {
            Keys = new []{""},
            UseForce = true
        };

        await DeleteHandler.DeleteAsync(input, mockHelper.MockConfiguration.Object,
            mockHelper.MockLogger.Object, m_MockSystemEnvironmentProvider.Object, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Information, null, null,
            DeleteHandler.k_DeletedSpecifiedKeysMsg);

        mockHelper.MockConfiguration.Verify(ex => ex
            .DeleteConfigArgumentsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ChecksEnvironmentVariablesAndLogsWarningWhenValueStillSetInEnvironmentVariables()
    {
        ConfigurationInput input = new ConfigurationInput
        {
            Keys = new []{Keys.ConfigKeys.ProjectId},
            UseForce = true
        };

        string errorMsg;
        m_MockSystemEnvironmentProvider.Setup(ex => ex
            .GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg)).Returns("value");

        await DeleteHandler.DeleteAsync(input, mockHelper.MockConfiguration.Object,
            mockHelper.MockLogger.Object, m_MockSystemEnvironmentProvider.Object, CancellationToken.None);

        m_MockSystemEnvironmentProvider.Verify(ex => ex
            .GetSystemEnvironmentVariable(It.IsAny<string>(), out errorMsg), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(mockHelper.MockLogger, LogLevel.Warning);
    }
}
