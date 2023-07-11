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
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class DeleteHandlerTests
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
    public async Task LoadDeleteAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeleteHandler.DeleteAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_CallsDeleteServiceAndLoggerWhenInputIsValid()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            ScriptName = TestValues.ValidScriptName
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockCloudCode.Setup(
                ex => ex.DeleteAsync(
                    TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.ValidScriptName, CancellationToken.None))
            .Returns(Task.CompletedTask);

        await DeleteHandler.DeleteAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudCode.Verify(
            ex => ex.DeleteAsync(
                TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.ValidScriptName, CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
