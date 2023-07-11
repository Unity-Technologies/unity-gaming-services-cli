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
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class UpdateHandlerTests
{
    static readonly List<ScriptParameter> k_Parameters = new();

    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCode = new();
    readonly Mock<ICloudCodeInputParser> m_MockInputParseService = new();
    readonly Mock<ICloudCodeScriptParser> m_MockCloudCodeScriptParser = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCode.Reset();
        m_MockInputParseService.Reset();
        m_MockLogger.Reset();
        m_MockCloudCodeScriptParser.Reset();
    }

    [Test]
    public async Task LoadUpdateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await UpdateHandler.UpdateAsync(
            null!, null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_CallsUpdateAsyncWhenInputIsValid()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            FilePath = TestValues.ValidFilepath,
            ScriptName = TestValues.ValidScriptName
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockInputParseService.Setup(x => x.LoadScriptCodeAsync(input, CancellationToken.None))
            .ReturnsAsync(TestValues.ValidCode);
        m_MockCloudCodeScriptParser.Setup(x => x.ParseScriptParametersAsync(TestValues.ValidCode, CancellationToken.None))
            .ReturnsAsync(new ParseScriptParametersResult(false, k_Parameters));
        m_MockInputParseService.SetupGet(x => x.CloudCodeScriptParser)
            .Returns(m_MockCloudCodeScriptParser.Object);

        await UpdateHandler.UpdateAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockInputParseService.Object,
            m_MockLogger.Object,
            (StatusContext)null!,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(CancellationToken.None), Times.Once);
        m_MockCloudCode.Verify(
            e => e.UpdateAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidScriptName,
                TestValues.ValidCode,
                k_Parameters,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
