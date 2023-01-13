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
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Handlers;

[TestFixture]
class CreateHandlerTests
{
    static readonly string k_ValidScriptType = ScriptType.API.ToString();
    static readonly string k_ValidScriptLanguage = Language.JS.ToString();

    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();
    readonly Mock<ICloudCodeService> m_MockCloudCode = new();
    readonly Mock<ICloudCodeInputParser> m_MockInputParseService = new();
    readonly Mock<ILogger> m_MockLogger = new();

    [SetUp]
    public void SetUp()
    {
        m_MockUnityEnvironment.Reset();
        m_MockCloudCode.Reset();
        m_MockInputParseService.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task LoadCreateAsync_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await CreateHandler.CreateAsync(
            null!, null!, null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_CallsCreateAsyncWhenInputIsValid()
    {
        CloudCodeInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            FilePath = TestValues.ValidFilepath,
            ScriptType = k_ValidScriptType,
            ScriptLanguage = k_ValidScriptLanguage,
            ScriptName = TestValues.ValidScriptName,
        };
        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync())
            .ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockInputParseService.Setup(x => x.ParseScriptType(input))
            .Returns(ScriptType.API);
        m_MockInputParseService.Setup(x => x.ParseLanguage(input))
            .Returns(Language.JS);
        m_MockInputParseService.Setup(x => x.LoadScriptCodeAsync(input, CancellationToken.None))
            .ReturnsAsync(TestValues.ValidCode);

        await CreateHandler.CreateAsync(
            input,
            m_MockUnityEnvironment.Object,
            m_MockCloudCode.Object,
            m_MockInputParseService.Object,
            m_MockLogger.Object,
            (StatusContext)null!,
            CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(), Times.Once);
        m_MockInputParseService.Verify(x => x.ParseScriptType(input), Times.Once);
        m_MockInputParseService.Verify(x => x.ParseLanguage(input), Times.Once);
        m_MockInputParseService.Verify(x => x.LoadScriptCodeAsync(input, CancellationToken.None), Times.Once);
        m_MockCloudCode.Verify(
            e => e.CreateAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                TestValues.ValidScriptName,
                ScriptType.API,
                Language.JS,
                TestValues.ValidCode,
                CancellationToken.None),
            Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
