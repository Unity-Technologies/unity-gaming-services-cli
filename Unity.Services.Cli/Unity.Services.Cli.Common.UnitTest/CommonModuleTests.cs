using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Features;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Gateway.IdentityApiV1.Generated.Api;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class CommonModuleTests
{
    CommandLineBuilder? m_CommandLineBuilder;
    List<ServiceDescriptor>? m_Services;
    Mock<IServiceCollection>? m_MockedServiceCollection;
    readonly Mock<ISystemEnvironmentProvider> m_MockSystemEnvironmentProvider;
    Parser? m_Parser;

    public CommonModuleTests()
    {
        m_MockSystemEnvironmentProvider = new();
    }

    [SetUp]
    public void SetUp()
    {
        m_MockSystemEnvironmentProvider.Reset();
        m_CommandLineBuilder = new(new RootCommand("Test root command"));
        m_Parser = m_CommandLineBuilder.UseHost(_ => Host.CreateDefaultBuilder(),
            host =>
            {
                CommonModule.ConfigureCommonServices(host, new Logger(), new StaticFeatures(), AnsiConsole.Create(new AnsiConsoleSettings()));

            }).UseDefaults().AddGlobalCommonOptions().Build();
        m_Parser!.InvokeAsync("");

        m_Services = new List<ServiceDescriptor>();
        m_MockedServiceCollection = new Mock<IServiceCollection>();
        m_MockedServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(descriptor => m_Services.Add(descriptor));
    }

    [Test]
    public void CreateAndRegisterCommonServicesSucceeds()
    {
        var expectedEndpoint = EndpointHelper.GetCurrentEndpointFor<UnityServicesGatewayEndpoints>();
        var services = new List<ServiceDescriptor>();
        var mockedServiceCollection = new Mock<IServiceCollection>();
        mockedServiceCollection.Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
        .Callback<ServiceDescriptor>(descriptor => services.Add(descriptor));

        CommonModule.CreateAndRegisterIdentityApiServices(mockedServiceCollection.Object);

        Assert.AreEqual(1, services.Count);
        var registeredDefaultApi = services[0];
        Assert.AreEqual(typeof(IEnvironmentApi), registeredDefaultApi.ServiceType);
        var defaultApi = (EnvironmentApi)registeredDefaultApi.ImplementationInstance!;
        Assert.AreEqual(expectedEndpoint, defaultApi.Configuration.BasePath);
    }

    [Test]
    public void CreateAndRegisterProgressBarServiceSucceeds()
    {
        CommonModule.CreateAndRegisterProgressBarService(m_MockedServiceCollection!.Object, null);
        Assert.AreEqual(1, m_Services!.Count);
    }

    [Test]
    public void CreateAndRegisterLoadingIndicatorServiceSucceeds()
    {
        CommonModule.CreateAndRegisterLoadingIndicatorService(m_MockedServiceCollection!.Object, null);
        Assert.AreEqual(1, m_Services!.Count);
    }

    [Test]
    public void CreateAndRegisterCliPromptServiceSucceeds()
    {
        CommonModule.CreateAndRegisterCliPromptService(m_MockedServiceCollection!.Object);
        Assert.AreEqual(1, m_Services!.Count);
    }

    [Test]
    public void CreateAndRegisterProgressBarServiceNoQuietAliasSetsConsole()
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings());
        CommonModule.CreateAndRegisterProgressBarService(m_MockedServiceCollection!.Object, console);
        ProgressBar progressBar = (ProgressBar)m_Services![0].ImplementationInstance!;
        Assert.IsNotNull(progressBar.k_AnsiConsole);
    }

    [Test]
    public void CreateAndRegisterProgressBarServiceQuietAliasSetsConsoleToNull()
    {
        IAnsiConsole? console = null;
        CommonModule.CreateAndRegisterProgressBarService(m_MockedServiceCollection!.Object, console);
        ProgressBar progressBar = (ProgressBar)m_Services![0].ImplementationInstance!;
        Assert.IsNull(progressBar.k_AnsiConsole);
    }


    [Test]
    public void CreateTelemetrySender_RegistersCicdPlatformAsCommonTag()
    {
        string? errorMsg = null;
        string expectedPlatform = Keys.CicdEnvVarToDisplayNamePair[Keys.EnvironmentKeys.RunningOnDocker];
        m_MockSystemEnvironmentProvider.Setup(ex => ex
                .GetSystemEnvironmentVariable(Keys.EnvironmentKeys.RunningOnDocker, out errorMsg))
            .Returns(expectedPlatform);
        var telemetrySender = CommonModule.CreateTelemetrySender(m_MockSystemEnvironmentProvider.Object);
        StringAssert.AreEqualIgnoringCase(telemetrySender.CommonTags[TagKeys.CicdPlatform], expectedPlatform);
    }

    [Test]
    public void CreateTelemetrySender_SetsCorrectBasePath()
    {
        var telemetrySender = CommonModule.CreateTelemetrySender(m_MockSystemEnvironmentProvider.Object);
        StringAssert.AreEqualIgnoringCase(telemetrySender.TelemetryApi.GetBasePath(),
            EndpointHelper.GetCurrentEndpointFor<TelemetryApiEndpoints>());
    }

    [Test]
    public void CreateTelemetrySender_SetsBaseProductTags()
    {
        var telemetrySender = CommonModule.CreateTelemetrySender(m_MockSystemEnvironmentProvider.Object);
        StringAssert.AreEqualIgnoringCase(telemetrySender.ProductTags[TagKeys.ProductName],"com.unity.ugs-cli");
        StringAssert.AreEqualIgnoringCase(telemetrySender.ProductTags[TagKeys.CliVersion], TelemetryConfigurationProvider.GetCliVersion());
    }
}
