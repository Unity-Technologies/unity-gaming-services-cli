using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.CloudCode.Handlers;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class GetModuleHandlerTests
{
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCode = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly DateTime dateTime = DateTime.Now;

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCode.Reset();
        m_MockLogger.Reset();

        m_MockCloudCode.Setup(
                c => c.GetModuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(
                new GetModuleResponse(
                    "foo",
                    "CS",
                    null,
                    "url",
                    dateTime,
                    dateTime));
    }

    [Test]
    public async Task LoadGetModuleAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetModuleHandler.GetModuleAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetModuleHandler_ValidInputLogsResult()
    {
        CloudCodeInput cloudCodeInput = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            ModuleName = "foo"
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await GetModuleHandler.GetModuleAsync(
            cloudCodeInput,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudCode.Verify(
            api => api.GetModuleAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                cloudCodeInput.ModuleName,
                CancellationToken.None),
            Times.Once);

        var output = new GetModuleResponseOutput(new GetModuleResponse(
            cloudCodeInput.ModuleName,
            "CS",
            null,
            "url",
            dateTime,
            dateTime));

        TestsHelper.VerifyLoggerWasCalled(
            m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once, output.ToString());
    }
}
