using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Deployment.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

[TestFixture]
public class FetchHandlerTests
{
    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    readonly Mock<IHost> m_Host = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<IServiceProvider> m_ServiceProvider = new();
    readonly Mock<IFetchService> m_FetchService = new();
    readonly Mock<ICliDeploymentDefinitionService> m_DdefService = new();
    readonly Mock<IAnalyticsEventBuilder> m_AnalyticsEventBuilder = new();
    readonly Mock<IUnityEnvironment> m_MockEnvironment = new();

    [SetUp]
    public void SetUp()
    {
        m_Host.Reset();
        m_ServiceProvider.Reset();
        m_FetchService.Reset();
        m_Logger.Reset();
        m_AnalyticsEventBuilder.Reset();
        m_MockEnvironment.Reset();

        m_MockEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None)).Returns(Task.FromResult(k_ValidEnvironmentId));
        m_FetchService.Setup(s => s.ServiceName)
            .Returns("mock_test");
        m_FetchService.Setup(s => s.FileExtensions)
            .Returns(
                new[]
                {
                    ".test"
                });

        m_FetchService.Setup(
                s => s.FetchAsync(
                    It.IsAny<FetchInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
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

        m_DdefService
            .Setup(
                x => x.GetFilesFromInput(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IEnumerable<string>>()))
            .Returns(
                new DeploymentDefinitionFilteringResult(
                    new DeploymentDefinitionFiles(),
                    new Dictionary<string, IReadOnlyList<string>>
                    {
                        { ".test", new List<string>() }
                    }));
    }

    class TestFetchService : IFetchService
    {
        string m_ServiceType = "Test";
        string m_ServiceName = "test";
        string m_DeployFileExtension = ".test";

        public string ServiceType => m_ServiceType;
        public string ServiceName => m_ServiceName;
        public IReadOnlyList<string> FileExtensions => new[]
        {
            m_DeployFileExtension
        };

        public Task<FetchResult> FetchAsync(
            FetchInput input,
            IReadOnlyList<string> filePaths,
            string projectId,
            string environmentId,
            StatusContext? loadingContext,
            CancellationToken cancellationToken)
        {
            var res = new FetchResult(
                StringsToDeployContent(new[] { "updated1" }),
                StringsToDeployContent(new[] { "deleted1" }),
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

        await FetchCommandHandler.FetchAsync(
            null!,
            null!,
            m_MockEnvironment.Object,
            null!,
            m_DdefService.Object,
            mockLoadingIndicator.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

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
        var mockDdefService = new Mock<ICliDeploymentDefinitionService>();

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            fetchInput,
            m_MockEnvironment.Object,
            mockLogger.Object,
            (StatusContext?)null,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
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

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            fetchInput,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

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
        collection.AddScoped<IFetchService, TestFetchUnhandledExceptionFetchService>();
        var provider = bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services).Returns(provider);
        var fetchInput = new FetchInput();
        Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await FetchCommandHandler.FetchAsync(
                m_Host.Object,
                fetchInput,
                m_MockEnvironment.Object,
                m_Logger.Object,
                (StatusContext)null!,
                m_DdefService.Object,
                m_AnalyticsEventBuilder.Object,
                CancellationToken.None);
        });
    }

    [Test]
    public async Task FetchAsync_ReconcileWillNotExecutedWithNoServiceFlag()
    {
        var input = new FetchInput()
        {
            Reconcile = true
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
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

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
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

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
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

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_MultipleDeploymentDefinitionsException_NotExecuted()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        m_DdefService
            .Setup(
                s => s.GetFilesFromInput(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>()))
            .Throws(
                () =>
                    new MultipleDeploymentDefinitionInDirectoryException(
                        new Mock<IDeploymentDefinition>().Object,
                        new Mock<IDeploymentDefinition>().Object,
                        "path"));

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_DeploymentDefinitionIntersectionException_NotExecuted()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        m_DdefService
            .Setup(
                s => s.GetFilesFromInput(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>()))
            .Throws(
                new DeploymentDefinitionFileIntersectionException(
                    new Dictionary<IDeploymentDefinition, List<string>>(),
                    true));

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task FetchAsync_DeploymentDefinitionsHaveExclusion_ExclusionsLogged()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        var mockResult = new Mock<IDeploymentDefinitionFilteringResult>();
        mockResult
            .Setup(r => r.AllFilesByExtension)
            .Returns(
                new Dictionary<string, IReadOnlyList<string>>
                {
                    { ".test", new List<string>() }
                });
        var mockFiles = new Mock<IDeploymentDefinitionFiles>();
        mockFiles
            .Setup(f => f.HasExcludes)
            .Returns(true);
        mockResult
            .Setup(r => r.DefinitionFiles)
            .Returns(mockFiles.Object);
        m_DdefService
            .Setup(
                s => s.GetFilesFromInput(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<IEnumerable<string>>()))
            .Returns(mockResult.Object);


        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        mockResult.Verify(r => r.GetExclusionsLogMessage(), Times.Once);
    }

    [Test]
    public async Task FetchAsync_ReconcileWithDdef_NotExecuted()
    {
        var input = new FetchInput()
        {
            Services = new[]
            {
                "mock_test"
            },
            Path = "some/path/to/A.ddef",
            Reconcile = true
        };

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            input,
            m_MockEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_FetchService.Verify(
            s => s.FetchAsync(
                It.IsAny<FetchInput>(),
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    class TestFetchUnhandledExceptionFetchService : IFetchService
    {
        string m_ServiceType = "Test";
        string m_ServiceName = "test";
        string m_DeployFileExtension = ".test";

        public string ServiceType => m_ServiceType;
        public string ServiceName => m_ServiceName;
        public IReadOnlyList<string> FileExtensions => new[]
        {
            m_DeployFileExtension
        };

        public Task<FetchResult> FetchAsync(
            FetchInput input,
            IReadOnlyList<string> filePaths,
            string projectId,
            string environmentId,
            StatusContext? loadingContext,
            CancellationToken cancellationToken)
        {
            return Task.FromException<FetchResult>(new NullReferenceException());
        }
    }
}
