using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Access.Handlers;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Access.UnitTest.Utils;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Unity.Services.Cli.Access.UnitTest.Handlers;

[TestFixture]
public class UpsertPlayerPolicyHandlerTests
{
    readonly Mock<IAccessService> m_MockAccessService = new();
    readonly Mock<ILogger> m_MockLogger = new();
    readonly Mock<IUnityEnvironment> m_MockUnityEnvironment = new();

    [SetUp]
    public void SetUp()
    {
        m_MockAccessService.Reset();
        m_MockLogger.Reset();
        m_MockUnityEnvironment.Reset();
    }

    [Test]
    public async Task UpsertPlayerPolicyAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();
        await UpsertPlayerPolicyHandler.UpsertPlayerPolicyAsync(null!, null!, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);
        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?,Task>>()), Times.Once);
    }

    [Test]
    public async Task UpsertPlayerPolicyHandler_valid()
    {
        AccessInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            FilePath = new FileInfo(TestValues.FilePath),
            PlayerId = TestValues.ValidPlayerId
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync()).ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockAccessService?.Setup(x =>
            x.UpsertPlayerPolicyAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.ValidPlayerId, It.IsAny<FileInfo>(),  CancellationToken.None));

        await UpsertPlayerPolicyHandler.UpsertPlayerPolicyAsync(input, m_MockUnityEnvironment.Object,
            m_MockAccessService!.Object, m_MockLogger!.Object, CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(), Times.Once);
        m_MockAccessService.Verify(x => x.UpsertPlayerPolicyAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.ValidPlayerId, It.IsAny<FileInfo>(), CancellationToken.None), Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
