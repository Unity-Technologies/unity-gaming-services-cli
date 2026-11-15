using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudCode.Handlers;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

class PublishHandlerTests
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
    public async Task LoadPublishAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await PublishHandler.PublishAsync(
            null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task PublishHandler_CallsPublishServiceAndLoggerWhenInputIsValid()
    {
        const int version = 2;
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            ScriptName = TestValues.ValidScriptName,
            Version = version,
        };
        var expectedResponse = new PublishScriptResponse(version);
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockCloudCode.Setup(
                x => x.PublishAsync(
                    TestValues.ValidProjectId,
                    TestValues.ValidEnvironmentId,
                    TestValues.ValidScriptName,
                    version,
                    CancellationToken.None))
            .ReturnsAsync(expectedResponse);

        await PublishHandler.PublishAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudCode.Verify(
            ex => ex.PublishAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidScriptName,
                version,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }

    [Test]
    public void PublishHandler_ConflictExplainsTheVersionLimit()
    {
        var conflict = new ApiException(409, "Conflict");
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            ScriptName = TestValues.ValidScriptName,
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockCloudCode.Setup(
                x => x.PublishAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(conflict);

        var ex = Assert.ThrowsAsync<CliException>(() => PublishHandler.PublishAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain($"'{TestValues.ValidScriptName}'"));
        Assert.That(ex.Message, Does.Contain("assigned to a release"));
        Assert.That(ex.ExitCode, Is.EqualTo(ExitCode.HandledError));
        Assert.That(ex.InnerException, Is.SameAs(conflict));
    }

    [Test]
    public void PublishHandler_OtherApiErrorsPropagateUnchanged()
    {
        var apiException = new ApiException(400, "Bad Request");
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            ScriptName = TestValues.ValidScriptName,
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockCloudCode.Setup(
                x => x.PublishAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(apiException);

        var ex = Assert.ThrowsAsync<ApiException>(() => PublishHandler.PublishAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None));

        Assert.That(ex, Is.SameAs(apiException));
    }
}
