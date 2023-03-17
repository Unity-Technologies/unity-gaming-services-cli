using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.CloudCode.Handlers;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class DeleteModuleHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCode = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCode.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task LoadDeleteModuleAsync_CallsLoadingIndicatorStartLoading()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeleteModuleHandler.DeleteModuleAsync(null!, null!, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task DeleteModuleAsync_CallsDeleteServiceAndLoggerWhenInputIsValid()
    {
        m_MockCloudCode.Setup(ex => ex
            .DeleteModuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        CloudCodeInput input = new()
        {
            CloudProjectId = "value",
            ScriptName = ""
        };

        await DeleteModuleHandler.DeleteModuleAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None
        );

        m_MockCloudCode.Verify(ex => ex
            .DeleteModuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);

        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, default, Times.Once);
    }
}
