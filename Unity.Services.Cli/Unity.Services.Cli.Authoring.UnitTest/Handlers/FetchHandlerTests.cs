using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.TestUtils;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

[TestFixture]
public class FetchHandlerTests
{
    readonly Mock<IHost> m_Host = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<IServiceProvider> m_ServiceProvider = new();
    readonly Mock<IFetchService> m_FetchService = new();

    [SetUp]
    public void SetUp()
    {
        m_Host.Reset();
        m_ServiceProvider.Reset();
        m_FetchService.Reset();
        m_Logger.Reset();

        m_FetchService.Setup(s => s.ServiceName)
            .Returns("mock_test");

        m_FetchService.Setup(
                s => s.FetchAsync(
                    It.IsAny<FetchInput>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(
                    new FetchResult(
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>())));

        var bridge = new ServiceTypesBridge();
        var collection = bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService, TestFetchService>();
        collection.AddScoped<IFetchService>((_) => m_FetchService.Object);
        var provider = bridge.CreateServiceProvider(collection);

        m_Host.Setup(x => x.Services)
            .Returns(provider);
    }

    class TestFetchService : IFetchService
    {
        string m_ServiceType = "Test";
        string m_ServiceName = "test";
        string m_DeployFileExtension = ".test";

        string IFetchService.ServiceType => m_ServiceType;
        string IFetchService.ServiceName => m_ServiceName;

        string IFetchService.FileExtension => m_DeployFileExtension;

        public Task<FetchResult> FetchAsync(FetchInput input, StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            var res = new FetchResult(
                StringsToDeployContent(new[] { "updated1" }),
                StringsToDeployContent (new[] { "deleted1" }),
                Array.Empty<DeployContent>(),
                StringsToDeployContent(new[] { "file1" }),
                Array.Empty<DeployContent>());
            return Task.FromResult(res);
        }
    }

    [Test]
    public async Task FetchAsync_WithLoadingIndicator_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await FetchHandler.FetchAsync(
            null!, null!, null!, mockLoadingIndicator.Object, CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }

    [Test]
    [TestCase(true, Description = "Dry-Run is dry")]
    [TestCase(false, Description = "Wet-Run is wet")]
    public async Task FetchAsync_PrintsCorrectDryRun(bool dryRun)
    {
        var mockLogger = new Mock<ILogger>();
        var fetchInput = new FetchInput { DryRun = dryRun };

        await FetchHandler.FetchAsync(
            m_Host.Object,
            fetchInput,
            mockLogger.Object,
            (StatusContext?)null,
            CancellationToken.None);

        mockLogger.Verify(l => l.Log(
            LogLevel.Critical,
            LoggerExtension.ResultEventId,
            It.Is<object>(
                (o, t) => ((FetchResult)o).DryRun == dryRun),
            null,
            It.IsAny<Func<object, Exception?, string>>()));
    }


    [Test]
    public async Task FetchAsync_CallsGetServicesCorrectly()
    {
        var fetchInput = new FetchInput();

        await FetchHandler.FetchAsync(
            m_Host.Object, fetchInput, m_Logger.Object, (StatusContext)null!, CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_Logger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    static IReadOnlyList<DeployContent> StringsToDeployContent(IEnumerable<string> strs)
    {
        return strs.Select(s => new DeployContent(s, string.Empty, string.Empty)).ToList();
    }

    [Test]
    public void FetchAsync_ThrowsAggregateException()
    {
        m_Host.Reset();
        var bridge = new ServiceTypesBridge();
        var collection = bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService, TestFetchUnhandleExceptionFetchService>();
        var provider = bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services).Returns(provider);
        var fetchInput = new FetchInput();
        Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await FetchHandler.FetchAsync(
                m_Host.Object, fetchInput, m_Logger.Object, (StatusContext)null!, CancellationToken.None);
        });
    }

    [Test]
    public async Task FetchAsync_ReconcileWillNotExecutedWithNoServiceFlag()
    {
        var input = new FetchInput()
        {
            Reconcile = true
        };

        await FetchHandler.FetchAsync(
            m_Host.Object,
            input,
            m_Logger.Object,
            (StatusContext)null!,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_ReconcileExecuteWithServiceFlag()
    {
        var input = new FetchInput()
        {
            Reconcile = true,
            Services = new[]
            {
                "mock_test"
            }
        };

        await FetchHandler.FetchAsync(
            m_Host.Object,
            input,
            m_Logger.Object,
            (StatusContext)null!,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_ExecuteWithCorrectServiceFlag()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        await FetchHandler.FetchAsync(
            m_Host.Object,
            input,
            m_Logger.Object,
            (StatusContext)null!,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchAsync_NotExecuteWithIncorrectServiceFlag()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "non-test"
            }
        };

        await FetchHandler.FetchAsync(
            m_Host.Object,
            input,
            m_Logger.Object,
            (StatusContext)null!,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    class TestFetchUnhandleExceptionFetchService : IFetchService
    {
        string m_ServiceType = "Test";
        string m_ServiceName = "test";
        string m_DeployFileExtension = ".test";

        string IFetchService.ServiceType => m_ServiceType;
        string IFetchService.ServiceName => m_ServiceName;

        string IFetchService.FileExtension => m_DeployFileExtension;

        public Task<FetchResult> FetchAsync(FetchInput input, StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            return Task.FromException<FetchResult>(new NullReferenceException());
        }
    }
}
