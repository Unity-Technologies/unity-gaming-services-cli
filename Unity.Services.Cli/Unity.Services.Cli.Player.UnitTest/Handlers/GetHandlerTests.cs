using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Player.Handlers;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Player.UnitTest.Handlers;

public class GetHandlerTests
{
    readonly Mock<IPlayerService>? m_MockPlayerService = new();
    readonly Mock<ILogger>? m_MockLogger = new();
    const string k_PlayerId = "player-id";
    const string k_ProjectId = "abcd1234-ab12-cd34-ef56-abcdef123456";

    [SetUp]
    public void SetUp()
    {
        m_MockPlayerService.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task GetAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await GetHandler.GetAsync(null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task GetHandler_Valid()
    {
        PlayerInput input = new()
        {
            PlayerId = k_PlayerId,
            CloudProjectId = k_ProjectId,
        };

        m_MockPlayerService?.Setup(x => x.GetAsync(k_ProjectId, k_PlayerId,
            CancellationToken.None));

        await GetHandler.GetAsync(
            input,
            m_MockPlayerService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockPlayerService.Verify(s => s.GetAsync(k_ProjectId, k_PlayerId, CancellationToken.None), Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, null, Times.Once);
    }
}
