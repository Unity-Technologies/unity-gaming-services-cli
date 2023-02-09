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
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Deploy.Handlers;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Deploy.UnitTest.Handlers;

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
        string m_DeployFileExtension = ".test";

        string IDeploymentService.ServiceType => m_ServiceType;

        string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

        public Task<DeploymentResult> Deploy(DeployInput deployInput, IReadOnlyList<string> filePaths, string projectId, string environmentId,
            StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeploymentResult(new List<DeployContent>(), new List<DeployContent>()));
        }
    }

    public class TestDeploymentFailureService : IDeploymentService
    {
        string m_ServiceType = "Test";
        string m_DeployFileExtension = ".test";

        string IDeploymentService.ServiceType => m_ServiceType;

        string IDeploymentService.DeployFileExtension => m_DeployFileExtension;

        public Task<DeploymentResult> Deploy(DeployInput deployInput, IReadOnlyList<string> filePaths, string projectId, string environmentId,
            StatusContext? loadingContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeploymentResult(
                new List<DeployContent>()
                {
                    new ("success", "type", "path_1")
                },
                new List<DeployContent>()
                {
                    new ("failure", "type", "path_2")
                }));
        }
    }

    public class TestDeploymentUnhandledExceptionService : IDeploymentService
    {
        string m_ServiceType = "Test";
        string m_DeployFileExtension = ".test";

        string IDeploymentService.ServiceType => m_ServiceType;

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
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IDeploymentService, TestDeploymentService>();
        var provider = m_Bridge.CreateServiceProvider(collection);

        m_Host.Setup(x => x.Services)
            .Returns(provider);

        m_UnityEnvironment.Setup(x => x.FetchIdentifierAsync()).Returns(Task.FromResult(ValidEnvironmentId));
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
}
