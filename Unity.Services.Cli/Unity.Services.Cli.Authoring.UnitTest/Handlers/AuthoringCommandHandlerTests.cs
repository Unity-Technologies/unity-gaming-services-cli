using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.UnitTest.Handlers;

public class AuthoringCommandHandlerTests
{
    readonly Mock<IHost> m_Host = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly Mock<IServiceProvider> m_ServiceProvider = new();
    readonly Mock<IUnityEnvironment> m_UnityEnvironment = new();
    readonly Mock<IAnalyticsEventBuilder> m_AnalyticsEventBuilder = new();
    readonly ServiceTypesBridge m_Bridge = new();

    const string k_ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";

    public interface ITest1Service  : IDeploymentService, IFetchService { }
    public interface ITest2Service  : IDeploymentService, IFetchService { }

    [SetUp]
    public void SetUp()
    {
        m_Host.Reset();
        m_ServiceProvider.Reset();
        m_Logger.Reset();
        m_UnityEnvironment.Reset();
        m_UnityEnvironment
            .Setup(x => x.FetchIdentifierAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_ValidEnvironmentId));
    }

    [Test]
    public async Task DeployDoesNotCallService_NotReconcile()
    {
        var mockService1 =
            (Mock<ITest1Service>)CreateMockDeploymentService<ITest1Service>(".test1", "Test1");
        var mockService2 =
            (Mock<ITest2Service>)CreateMockDeploymentService<ITest2Service>(".test2", "Test2");
        m_Host.Reset();
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IDeploymentService>(_ => mockService1.Object);
        collection.AddScoped<IDeploymentService>(_ => mockService2.Object);
        var provider = m_Bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services)
            .Returns(provider);

    var ddefServiceMock = GetDdefServiceMock(new Dictionary<string, IReadOnlyList<string>>
        {
            { ".test1", new[] {"path1.test1"}},
            { ".test2", Array.Empty<string>() }
        });

    await DeployCommandHandler.DeployAsync(
            m_Host.Object, new DeployInput { Reconcile = false , Services = new [] { "Test1", "Test2" } },
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            ddefServiceMock.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Verify services are called / not
        mockService1.Verify(serv1 => serv1
            .Deploy(It.IsAny<DeployInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        mockService2.Verify(serv2 => serv2
                .Deploy(It.IsAny<DeployInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task DeployDoesCallAllServices_OnReconcile()
    {
        var mockService1 = (Mock<ITest1Service>)CreateMockDeploymentService<ITest1Service>(".test1", "Test1");
        var mockService2 = (Mock<ITest2Service>)CreateMockDeploymentService<ITest2Service>(".test2", "Test2");
        m_Host.Reset();
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IDeploymentService>(_ => mockService1.Object);
        collection.AddScoped<IDeploymentService>(_ => mockService2.Object);
        var provider = m_Bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services)
            .Returns(provider);

        var ddefServiceMock = GetDdefServiceMock(new Dictionary<string, IReadOnlyList<string>>
        {
            { ".test1", new[] {"path1.test1"}},
            { ".test2", Array.Empty<string>() }
        });

        await DeployCommandHandler.DeployAsync(
            m_Host.Object, new DeployInput { Reconcile = true , Services = new [] { "Test1", "Test2" } },
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            ddefServiceMock.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Verify services are called / not
        mockService1.Verify(serv1 => serv1
                .Deploy(It.IsAny<DeployInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        mockService2.Verify(serv2 => serv2
                .Deploy(It.IsAny<DeployInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task FetchDoesNotCallServiceNotReconcile()
    {
        var mockService1 =
            (Mock<ITest1Service>)CreateMockFetchService<ITest1Service>(".test1", "Test1");
        var mockService2 =
            (Mock<ITest2Service>)CreateMockFetchService<ITest2Service>(".test2", "Test2");
        m_Host.Reset();
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService>(_ => mockService1.Object);
        collection.AddScoped<IFetchService>(_ => mockService2.Object);
        var provider = m_Bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services)
            .Returns(provider);

    var ddefServiceMock = GetDdefServiceMock(new Dictionary<string, IReadOnlyList<string>>
        {
            { ".test1", new[] {"path1.test1"}},
            { ".test2", Array.Empty<string>()}
        });

    await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            new FetchInput() { Reconcile = false , Services = new [] { "Test1", "Test2" } },
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            ddefServiceMock.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Verify services are called / not
        mockService1.Verify(serv1 => serv1
            .FetchAsync(It.IsAny<FetchInput>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<StatusContext?>(),
                It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        mockService2.Verify(serv2 => serv2
                .FetchAsync(It.IsAny<FetchInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task FetchDoesCallAllServicesOnReconcile()
    {
        var mockService1 = (Mock<ITest1Service>)CreateMockFetchService<ITest1Service>(".test1", "Test1");
        var mockService2 = (Mock<ITest2Service>)CreateMockFetchService<ITest2Service>(".test2", "Test2");
        m_Host.Reset();
        var collection = m_Bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<IFetchService>(_ => mockService1.Object);
        collection.AddScoped<IFetchService>(_ => mockService2.Object);
        var provider = m_Bridge.CreateServiceProvider(collection);
        m_Host.Setup(x => x.Services)
            .Returns(provider);

        var ddefServiceMock = GetDdefServiceMock(new Dictionary<string, IReadOnlyList<string>>
        {
            { ".test1", new[] {"path1.test1"}},
            { ".test2", Array.Empty<string>()}
        });

        await FetchCommandHandler.FetchAsync(
            m_Host.Object,
            new FetchInput { Reconcile = true , Services = new [] { "Test1", "Test2" } },
            m_UnityEnvironment.Object,
            m_Logger.Object,
            (StatusContext)null!,
            ddefServiceMock.Object,
            m_AnalyticsEventBuilder.Object,
            CancellationToken.None);

        // Verify services are called / not
        mockService1.Verify(serv1 => serv1
                .FetchAsync(It.IsAny<FetchInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        mockService2.Verify(serv2 => serv2
                .FetchAsync(It.IsAny<FetchInput>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<StatusContext?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    static IMock<IDeploymentService> CreateMockDeploymentService<T>(string fileExtension, string serviceName)
        where T : class, IDeploymentService
    {
        var deploymentService = new Mock<T>();
        deploymentService.Setup(s => s.ServiceName)
            .Returns(serviceName);
        deploymentService.Setup(s => s.FileExtensions)
            .Returns(
                new[]
                {
                    fileExtension
                });

        deploymentService.Setup(
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
        return deploymentService;
    }

    static IMock<IFetchService> CreateMockFetchService<T>(string fileExtension, string serviceName)
        where T : class, IFetchService
    {
        var fetchService = new Mock<T>();
        fetchService.Setup(s => s.ServiceName)
            .Returns(serviceName);
        fetchService.Setup(s => s.FileExtensions)
            .Returns(
                new[]
                {
                    fileExtension
                });

        fetchService.Setup(
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
        return fetchService;
    }

    static Mock<ICliDeploymentDefinitionService> GetDdefServiceMock(Dictionary<string, IReadOnlyList<string>> filesByExtension)
    {
        var ddefServiceMock = new Mock<ICliDeploymentDefinitionService>();
        ddefServiceMock
            .Setup(
                x => x.GetFilesFromInput(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IEnumerable<string>>()))
            .Returns(
                new DeploymentDefinitionFilteringResult(
                    new DeploymentDefinitionFiles(),
                    filesByExtension));
        return ddefServiceMock;
    }
}
