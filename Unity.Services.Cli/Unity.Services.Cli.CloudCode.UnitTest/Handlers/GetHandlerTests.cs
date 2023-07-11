using System;
using System.Collections.Generic;
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
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class GetHandlerTests
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

        m_MockCloudCode.Setup(
                c => c.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(
                new GetScriptResponse(
                    "foo",
                    ScriptType.API,
                    Language.JS,
                    new GetScriptResponseActiveScript("bar", 1, DateTime.Now, new List<ScriptParameter>()),
                    _params: new List<ScriptParameter>(),
                    versions: new List<GetScriptResponseVersionsInner>
                    {
                        new("bar", 1)
                    }));
    }

    [Test]
    public async Task LoadGetAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetHandler.GetAsync(null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetHandler_ValidInputLogsResult()
    {
        CloudCodeInput cloudCodeInput = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            ScriptName = "test"
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);

        await GetHandler.GetAsync(
            cloudCodeInput,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockLogger.Object,
            CancellationToken.None
        );

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudCode.Verify(
            api => api.GetAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                cloudCodeInput.ScriptName,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(
            m_MockLogger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }
}
