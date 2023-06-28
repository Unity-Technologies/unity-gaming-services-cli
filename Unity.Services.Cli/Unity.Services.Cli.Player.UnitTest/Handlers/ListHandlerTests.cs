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

public class ListHandlerTests
{
    readonly Mock<IPlayerService>? m_MockPlayerService = new();
    readonly Mock<ILogger>? m_MockLogger = new();
    const string k_ProjectId = "abcd1234-ab12-cd34-ef56-abcdef123456";
    const int k_Limit = 99;
    const string k_Page = "69y4J1gwcT45g23";

    [SetUp]
    public void SetUp()
    {
        m_MockPlayerService.Reset();
        m_MockLogger.Reset();
    }

    [Test]
    public async Task ListAsync_CallsLoadingIndicator()
    {
        Mock<ILoadingIndicator> mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await ListHandler.ListAsync(null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(ex => ex
            .StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    public async Task ListHandlerWithoutOption_Valid()
    {
        PlayerInput input = new()
        {
            Limit = k_Limit,
            CloudProjectId = k_ProjectId,
        };

        m_MockPlayerService?.Setup(x => x.ListAsync(k_ProjectId, k_Limit, null,
            CancellationToken.None));

        await ListHandler.ListAsync(
            input,
            m_MockPlayerService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockPlayerService.Verify(s => s.ListAsync(k_ProjectId, k_Limit, null, CancellationToken.None), Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, null, Times.Once);
    }

    [Test]
    public async Task ListHandlerWithOption_Valid()
    {
        PlayerInput input = new()
        {
            Limit = k_Limit,
            PlayersPage = k_Page,
            CloudProjectId = k_ProjectId,
        };

        m_MockPlayerService?.Setup(x => x.ListAsync(k_ProjectId, k_Limit, k_Page,
            CancellationToken.None));

        await ListHandler.ListAsync(
            input,
            m_MockPlayerService!.Object,
            m_MockLogger!.Object,
            CancellationToken.None
        );

        m_MockPlayerService.Verify(s => s.ListAsync(k_ProjectId, k_Limit, k_Page, CancellationToken.None), Times.Once);
        TestsHelper.VerifyLoggerWasCalled(m_MockLogger, LogLevel.Critical, null, Times.Once);
    }
}
