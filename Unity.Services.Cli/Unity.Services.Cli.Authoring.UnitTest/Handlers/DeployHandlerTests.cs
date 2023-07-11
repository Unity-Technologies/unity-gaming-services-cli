using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.TestUtils;
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
    readonly Mock<IDeployFileService> m_DeployFileService = new();
    readonly ServiceTypesBridge m_Bridge = new();

    const string ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";
    public class TestDeploymentService : IDeploymentService
    {
        string m_ServiceType = "Test";
        string m_ServiceName = "test";
        string m_DeployFileExtension = ".test";

        string IDeploymentService.ServiceType => m_ServiceType;
        string IDeploymentService.ServiceName => m_ServiceName;

        string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

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

        string IDeploymentService.ServiceType => m_ServiceType;
        string IDeploymentService.ServiceName => m_ServiceName;

        string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

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

        string IDeploymentService.ServiceType => m_ServiceType;
        string IDeploymentService.ServiceName => m_ServiceName;

        string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

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
        m_DeployFileService.Reset();

        m_DeploymentService.Setup(s => s.ServiceName)
            .Returns("mock_test");

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

        m_UnityEnvironment.Setup(x => x.FetchIdentifierAsync(CancellationToken.None)).Returns(Task.FromResult(ValidEnvironmentId));
        m_DeployFileService.Setup(x => x.ListFilesToDeploy(new[]
        {
            ""
        }, "*.ext")).Returns(new[]
        {
            ""
        });
    }

    [Test]
    public async Task DeployAsync_WithLoadingIndicator_CallsLoadingIndicatorStartLoading()
    {
        var mockLoadingIndicator = new Mock<ILoadingIndicator>();

        await DeployHandler.DeployAsync(
            It.IsAny<IHost>(),
            It.IsAny<DeployInput>(),
            m_DeployFileService.Object,
            m_UnityEnvironment.Object,
            It.IsAny<ILogger>(),
            mockLoadingIndicator.Object,
            CancellationToken.None);

        mockLoadingIndicator.Verify(
            ex => ex.StartLoadingAsync(It.IsAny<string>(), It.IsAny<Func<StatusContext?, Task>>()), Times.Once);
    }


    [Test]
    public async Task DeployAsync_CallsGetServicesCorrectly()
    {
        await DeployHandler.DeployAsync(
            m_Host.Object, new DeployInput(),
            m_DeployFileService.Object,
            m_UnityEnvironment.Object,
            m_Logger.Object, (StatusContext)null!, CancellationToken.None);

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
            await DeployHandler.DeployAsync(
                m_Host.Object, new DeployInput(),
                m_DeployFileService.Object,
                m_UnityEnvironment.Object,
                m_Logger.Object, (StatusContext)null!, CancellationToken.None);
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
            await DeployHandler.DeployAsync(
                m_Host.Object, new DeployInput(),
                m_DeployFileService.Object,
                m_UnityEnvironment.Object,
                m_Logger.Object, (StatusContext)null!, CancellationToken.None);
        });

    }

    [Test]
    public async Task DeployAsync_ReconcileWillNotExecutedWithNoServiceFlag()
    {
        var input = new DeployInput()
        {
            Reconcile = true
        };

        await DeployHandler.DeployAsync(
            m_Host.Object,
            input,
            m_DeployFileService.Object,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
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

        await DeployHandler.DeployAsync(
            m_Host.Object,
            input,
            m_DeployFileService.Object,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
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

        await DeployHandler.DeployAsync(
            m_Host.Object,
            input,
            m_DeployFileService.Object,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
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

        await DeployHandler.DeployAsync(
            m_Host.Object,
            input,
            m_DeployFileService.Object,
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
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
}
