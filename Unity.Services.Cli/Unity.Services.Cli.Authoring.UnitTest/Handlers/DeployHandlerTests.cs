using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Deployment.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

[TestFixture]
public class DeployHandlerTests
{
    readonly Mock<IHost> m_Host = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<IServiceProvider> m_ServiceProvider = new();
    readonly Mock<IDeploymentService> m_DeploymentService = new();
    readonly Mock<IUnityEnvironment> m_UnityEnvironment = new();
    readonly Mock<ICliDeploymentDefinitionService> m_DdefService = new();
    readonly Mock<IAnalyticsEventBuilder> m_AnalyticsEventBuilder = new();
    readonly ServiceTypesBridge m_Bridge = new();

    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    public class TestDeploymentService : IDeploymentService
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

        public Task<DeploymentResult> Deploy(DeployInput deployInput, IReadOnlyList<string> filePaths, string projectId, string environmentId,
            StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeploymentResult(
                    new List<DeployContent>(),
                    new List<DeployContent>(),
                    new List<DeployContent>(),
                    new List<DeployContent>(),
                    new List<DeployContent>(),
                    false));
        }
    }

    public class TestDeploymentFailureService : IDeploymentService
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

        public Task<DeploymentResult> Deploy(DeployInput deployInput, IReadOnlyList<string> filePaths, string projectId, string environmentId,
            StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeploymentResult(
                new List<DeployContent>(),
                new List<DeployContent>(),
                new List<DeployContent>(),
                new List<DeployContent>()
                {
                    new ("success", "type", "path_1")
                },
                new List<DeployContent>()
                {
                    new ("failure", "type", "path_2")
                },
                false));
        }
    }

    public class TestDeploymentUnhandledExceptionService : IDeploymentService
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

        public Task<DeploymentResult> Deploy(DeployInput deployInput, IReadOnlyList<string> filePaths, string projectId, string environmentId,
            StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            return Task.FromException<DeploymentResult>(new NotImplementedException());

        }
    }

    [SetUp]
    public void SetUp()
    {
        m_Host.Reset();
        m_ServiceProvider.Reset();
        m_DeploymentService.Reset();
        m_Logger.Reset();
        m_UnityEnvironment.Reset();
        m_DdefService.Reset();

        m_DeploymentService.Setup(s => s.ServiceName)
            .Returns("mock_test");
        m_DeploymentService.Setup(s => s.FileExtensions)
            .Returns(
                new[]
                {
                    ".test"
                });

        m_DeploymentService.Setup(
                s => s.Deploy(
                    It.IsAny<DeployInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(
                    new DeploymentResult(
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>(),
                        Array.Empty<IDeploymentItem>())));

        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IDeploymentService, TestDeploymentService>();
        collection.AddScoped<IDeploymentService>((_) => m_DeploymentService.Object);
        var provider = m_Bridge.CreateServiceProvider(collection);

        m_Host.Setup(x => x.Services)
            .Returns(provider);

        m_UnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None)).Returns(Task.FromResult(k_ValidEnvironmentId));
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

    [Test]
    public async Task DeployAsync_WithLoadingIndicator_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeployCommandHandler.DeployAsync(
            It.IsAny<IHost>(),
            It.IsAny<DeployInput>(),
            m_UnityEnvironment.Object,
            It.IsAny<ILogger>(),
            mockLoadingIndicator.Object,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }


    [Test]
    public async Task DeployAsync_CallsGetServicesCorrectly()
    {
        await DeployCommandHandler.DeployAsync(
            m_Host.Object, new DeployInput(),
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        TestsHelper.VerifyLoggerWasCalled(m_Logger, LogLevel.Critical, LoggerExtension.ResultEventId, Times.Once);
    }

    [Test]
    public void DeployAsync_DeploymentFailureThrowsDeploymentFailureException()
    {
        m_Host.Reset();
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IDeploymentService, TestDeploymentFailureService>();
        var provider = m_Bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services)
            .Returns(provider);

        Assert.ThrowsAsync<DeploymentFailureException>(async () =>
        {
            await DeployCommandHandler.DeployAsync(
                m_Host.Object, new DeployInput(),
                m_UnityEnvironment.Object,
                m_Logger.Object,
                (StatusContext)null!,
                m_DdefService.Object,
                m_AnalyticsEventBuilder.Object,
                CancellationToken.None);
        });
    }

    [Test]
    public void DeployAsync_DeploymentFailureThrowsAggregateException()
    {
        m_Host.Reset();
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IDeploymentService, TestDeploymentUnhandledExceptionService>();
        var provider = m_Bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services)
            .Returns(provider);

        Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await DeployCommandHandler.DeployAsync(
                m_Host.Object, new DeployInput(),
                m_UnityEnvironment.Object,
                m_Logger.Object,
                (StatusContext)null!,
                m_DdefService.Object,
                m_AnalyticsEventBuilder.Object,
                CancellationToken.None);
        });

    }

    [Test]
    public async Task DeployAsync_ReconcileWillNotExecutedWithNoServiceFlag()
    {
        var input = new DeployInput()
        {
            Reconcile = true
        };

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_DeploymentService.Verify(
            s => s.Deploy(
                It.IsAny<DeployInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeployAsync_ReconcileExecuteWithServiceFlag()
    {
        var input = new DeployInput()
        {
            Reconcile = true,
            Services = new[]
            {
                "mock_test"
            }
        };

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_DeploymentService.Verify(
            s => s.Deploy(
                It.IsAny<DeployInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_ExecuteWithCorrectServiceFlag()
    {
        var input = new DeployInput()
        {
            Services = new[]
            {
                "mock_test"
            }
        };

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_DeploymentService.Verify(
            s => s.Deploy(
                It.IsAny<DeployInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeployAsync_NotExecuteWithIncorrectServiceFlag()
    {
        var input = new DeployInput()
        {
            Services = new[]
            {
                "non-test"
            }
        };

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_DeploymentService.Verify(
            s => s.Deploy(
                It.IsAny<DeployInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeployAsync_TableOutputWithJsonFlag()
    {
        var input = new DeployInput()
        {
            IsJson = true
        };

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        var tableResult = new DeploymentResult(
                Array.Empty<IDeploymentItem>(),
                Array.Empty<IDeploymentItem>(),
                Array.Empty<IDeploymentItem>(),
                Array.Empty<IDeploymentItem>(),
                Array.Empty<IDeploymentItem>())
            .ToTable();

        TestsHelper.VerifyLoggerWasCalled(m_Logger, message: tableResult.ToString());
    }

    [Test]
    public async Task DeployAsync_MultipleDeploymentDefinitionsException_NotExecuted()
    {
        var input = new DeployInput()
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

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_DeploymentService.Verify(
            s => s.Deploy(
                It.IsAny<DeployInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeployAsync_DeploymentDefinitionIntersectionException_NotExecuted()
    {
        var input = new DeployInput()
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

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        m_DeploymentService.Verify(
            s => s.Deploy(
                It.IsAny<DeployInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeployAsync_DeploymentDefinitionsHaveExclusion_ExclusionsLogged()
    {
        var input = new DeployInput()
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

        await DeployCommandHandler.DeployAsync(
            m_Host.Object,
            input,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            m_DdefService.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        mockResult.Verify(r => r.GetExclusionsLogMessage(), Times.Once);
    }
}
