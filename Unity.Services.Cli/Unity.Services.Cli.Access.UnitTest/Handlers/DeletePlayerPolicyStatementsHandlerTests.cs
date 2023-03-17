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
public class DeletePlayerPolicyStatementsHandlerTests
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
    public async Task DeletePlayerPolicyStatementsAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();
        await DeletePlayerPolicyStatementsHandler.DeletePlayerPolicyStatementsAsync(null!, null!, null!, null!,
            mockLoadingIndicator.Object, CancellationToken.None);
        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?,Task>>()), Times.Once);
    }

    [Test]
    public async Task DeletePlayerPolicyStatementsHandler_valid()
    {
        AccessInput input = new()
        {
            CloudProjectId = TestValues.ValidProjectId,
            PlayerId = TestValues.ValidPlayerId,
            FilePath = new FileInfo(TestValues.FilePath)
        };

        m_MockUnityEnvironment.Setup(x => x.FetchIdentifierAsync()).ReturnsAsync(TestValues.ValidEnvironmentId);
        m_MockAccessService?.Setup(x =>
            x.DeletePlayerPolicyStatementsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.ValidPlayerId, It.IsAny<FileInfo>(),  CancellationToken.None));

        await DeletePlayerPolicyStatementsHandler.DeletePlayerPolicyStatementsAsync(input, m_MockUnityEnvironment.Object,
            m_MockAccessService!.Object, m_MockLogger!.Object, CancellationToken.None);

        m_MockUnityEnvironment.Verify(x => x.FetchIdentifierAsync(), Times.Once);
        m_MockAccessService.Verify(x => x.DeletePlayerPolicyStatementsAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, TestValues.ValidPlayerId, It.IsAny<FileInfo>(), CancellationToken.None), Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Information, expectedTimes: Times.Once);
    }
}
