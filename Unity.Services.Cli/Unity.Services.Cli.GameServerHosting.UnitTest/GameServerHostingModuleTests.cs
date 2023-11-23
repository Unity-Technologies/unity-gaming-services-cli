using System.CommandLine.Builder;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.GameServerHosting.Endpoints;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Multiplay.Authoring.Core;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.UnitTest;

public class GameServerHostingModuleTests
{
    static readonly GameServerHostingModule k_GshModule = new();

    [Test]
    public void ValidateBuildCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_GshModule);

        TestsHelper.AssertContainsCommand(
            k_GshModule.ModuleRootCommand,
            k_GshModule.BuildCommand.Name,
            out var resultCommand
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(resultCommand, Is.EqualTo(k_GshModule.BuildCommand));
                Assert.That(k_GshModule.BuildCreateCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.BuildDeleteCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.BuildGetCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.BuildInstallsCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.BuildListCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.BuildUpdateCommand.Handler, Is.Not.Null);
            }
        );
    }

    [Test]
    public void ValidateFleetCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_GshModule);

        TestsHelper.AssertContainsCommand(
            k_GshModule.ModuleRootCommand,
            k_GshModule.FleetCommand.Name,
            out var resultCommand
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(resultCommand, Is.EqualTo(k_GshModule.FleetCommand));
                Assert.That(k_GshModule.FleetCreateCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.FleetDeleteCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.FleetGetCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.FleetListCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.FleetUpdateCommand.Handler, Is.Not.Null);
            }
        );
    }

    [Test]
    public void ValidateFleetRegionCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_GshModule);
        TestsHelper.AssertContainsCommand(
            k_GshModule.ModuleRootCommand,
            k_GshModule.FleetRegionCommand.Name,
            out var resultCommand
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(resultCommand, Is.EqualTo(k_GshModule.FleetRegionCommand));
                Assert.That(k_GshModule.FleetRegionTemplatesCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.FleetRegionAvailableCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.FleetRegionCreateCommand.Handler, Is.Not.Null);
            }
        );
    }

    [Test]
    public void ValidateServerCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_GshModule);
        TestsHelper.AssertContainsCommand(
            k_GshModule.ModuleRootCommand,
            k_GshModule.ServerCommand.Name,
            out var resultCommand
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(resultCommand, Is.EqualTo(k_GshModule.ServerCommand));
                Assert.That(k_GshModule.ServerGetCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.ServerListCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.ServerFilesListCommand.Handler, Is.Not.Null);
                Assert.That(k_GshModule.ServerFilesDownloadCommand.Handler, Is.Not.Null);
            }
        );
    }

    [Test]
    public void ExceptionFactory_NullException()
    {
        var response = new ApiResponse<string>(HttpStatusCode.OK, "data", "raw content");

        var exception = GameServerHostingModule.ExceptionFactory("test", response);

        Assert.That(exception, Is.Null);
    }

    [TestCase(HttpStatusCode.BadRequest, "Bad Request")]
    [TestCase(HttpStatusCode.OK, "Failed Deserialization")]
    public void ExceptionFactory_CreateException(HttpStatusCode statusCode, string errorText)
    {
        const string methodName = "test";
        const string rawContent = "raw content";
        const string data = "data";

        var response = new ApiResponse<string>(statusCode, data, rawContent)
        {
            ErrorText = errorText
        };

        var exception = GameServerHostingModule.ExceptionFactory(methodName, response);

        var expectedMessage = statusCode >= HttpStatusCode.BadRequest
            ? $"Error calling {methodName}: {rawContent}"
            : $"Error calling {methodName}: {errorText}";

        var expectedContent = statusCode >= HttpStatusCode.BadRequest
            ? rawContent
            : data;

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception, Is.TypeOf(typeof(ApiException)));
        var apiException = (ApiException)exception;
        Assert.Multiple(
            () =>
            {
                Assert.That(apiException.ErrorCode, Is.EqualTo((int)statusCode));
                Assert.That(apiException.Message, Is.EqualTo(expectedMessage));
                Assert.That(apiException.ErrorContent, Is.EqualTo(expectedContent));
            });
    }

    [TestCase(typeof(IBuildsApiAsync))]
    [TestCase(typeof(IBuildConfigurationsApiAsync))]
    [TestCase(typeof(IFleetsApiAsync))]
    [TestCase(typeof(GameServerHostingApiConfig))]
    [TestCase(typeof(IBuildsApiFactory))]
    [TestCase(typeof(IBuildConfigApiFactory))]
    [TestCase(typeof(IFleetApiFactory))]
    [TestCase(typeof(IGameServerHostingService))]
    [TestCase(typeof(MultiplayDeployer))]
    [TestCase(typeof(IDeploymentFacadeFactory))]
    [TestCase(typeof(IDeploymentFacade))]
    [TestCase(typeof(IMultiplayBuildAuthoring))]
    [TestCase(typeof(IBinaryBuilder))]
    [TestCase(typeof(IBuildFileManagement))]
    public void GameServerHostingModule_RegistersServices(Type serviceType)
    {
        var types = new List<TypeInfo>
        {
            typeof(UnityServicesGatewayEndpoints).GetTypeInfo(),
            typeof(CloudContentDeliveryEndpoints).GetTypeInfo()
        };
        EndpointHelper.InitializeNetworkTargetEndpoints(types);
        var collection = new ServiceCollection();
        collection.AddSingleton(new Mock<IServiceAccountAuthenticationService>().Object);
        GameServerHostingModule.RegisterServices(new HostBuilderContext(new Dictionary<object, object>()), collection);

        Assert.That(collection.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }

    [Test]
    public void ConfigureGameServerHostingRegistersExpectedServices()
    {
        var services = new List<ServiceDescriptor>
        {
            ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object)
        };

        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        EndpointHelper.InitializeNetworkTargetEndpoints(
            new[]
            {
                typeof(UnityServicesGatewayEndpoints).GetTypeInfo(),
                typeof(CloudContentDeliveryEndpoints).GetTypeInfo()
            });
        hostBuilder.ConfigureServices(GameServerHostingModule.RegisterServices);

        TestsHelper.AssertHasServiceSingleton<IGameServerHostingService, GameServerHostingService>(services);
    }
}
