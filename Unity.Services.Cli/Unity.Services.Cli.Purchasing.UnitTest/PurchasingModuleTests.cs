using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Purchasing.Input;
using Unity.Services.Cli.ServiceAccountAuthentication;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Api;

namespace Unity.Services.Cli.Purchasing.UnitTest;

[TestFixture]
class PurchasingModuleTests
{
    static readonly PurchasingModule k_Module = new();

    [Test]
    public void ModuleRootCommand_IsNotNull()
    {
        Assert.That(k_Module.ModuleRootCommand, Is.Not.Null);
        Assert.That(k_Module.ModuleRootCommand!.Name, Is.EqualTo("purchasing"));
    }

    [Test]
    public void ModuleRootCommand_HasIapAlias()
    {
        Assert.That(k_Module.ModuleRootCommand!.Aliases, Contains.Item("iap"));
    }

    [Test]
    public void ModuleRootCommand_HasNewFileSubcommand()
    {
        Assert.That(
            k_Module.ModuleRootCommand!.Subcommands.Any(c => c.Name == "new-file"),
            Is.True);
    }

    [Test]
    public void NewFileCommand_HasCsvOption()
    {
        var newFileCmd = k_Module.ModuleRootCommand!.Subcommands
            .First(c => c.Name == "new-file");

        Assert.That(newFileCmd.Options.Contains(PurchasingNewFileInput.CsvOption));
    }

    [Test]
    public void ListCommand_HasCloudProjectIdOption()
    {
        var listCmd = k_Module.ModuleRootCommand!.Subcommands
            .First(c => c.Name == "list");

        Assert.That(listCmd.Options.Contains(CommonInput.CloudProjectIdOption));
    }

    [Test]
    public void ListCommand_HasEnvironmentNameOption()
    {
        var listCmd = k_Module.ModuleRootCommand!.Subcommands
            .First(c => c.Name == "list");

        Assert.That(listCmd.Options.Contains(CommonInput.EnvironmentNameOption));
    }

    [TestCase(typeof(IDeploymentService))]
    [TestCase(typeof(IFetchService))]
    [TestCase(typeof(ILiveContentConfigClient))]
    [TestCase(typeof(ICatalogUcatLoader))]
    [TestCase(typeof(ICatalogCsvParser))]
    [TestCase(typeof(ICatalogCsvLoader))]
    public void RegisterServices_RegistersExpectedServices(Type serviceType)
    {
        InitializeEndpoints();

        var collection = new ServiceCollection();
        collection.AddSingleton(
            new Mock<IServiceAccountAuthenticationService>().Object);

        PurchasingModule.RegisterServices(collection);

        Assert.That(
            collection.Any(d => d.ServiceType == serviceType),
            Is.True,
            $"{serviceType.Name} was not registered");
    }

    [Test]
    public void RegisterServices_AddsIapFeatureFlagHeaderToLiveContentClient()
    {
        InitializeEndpoints();

        var collection = new ServiceCollection();
        PurchasingModule.RegisterServices(collection);
        using var provider = collection.BuildServiceProvider();

        var api = provider.GetRequiredService<IConfigsApiAsync>();

        Assert.That(
            api.Configuration.DefaultHeaders["X-Feature-Flag"],
            Is.EqualTo("file-repo-v2"));
    }

    [Test]
    public void RegisterServices_ResolvesLiveContentConfigClient()
    {
        InitializeEndpoints();

        var collection = new ServiceCollection();
        collection.AddSingleton(new Mock<IServiceAccountAuthenticationService>().Object);
        collection.AddSingleton(new Mock<ILogger>().Object);
        PurchasingModule.RegisterServices(collection);
        using var provider = collection.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<ILiveContentConfigClient>(), Is.Not.Null);
    }

    [Test]
    public void GetSchemaRegistryBasePath_StripsVersionSegmentFromEndpoint()
    {
        InitializeEndpoints();

        var endpoint = EndpointHelper.GetCurrentEndpointFor<SchemaRegistryApiEndpoints>();
        var basePath = PurchasingModule.GetSchemaRegistryBasePath();

        Assert.That(endpoint, Does.StartWith(basePath));
        Assert.That(basePath, Does.Not.EndWith("/v1"));
        Assert.That(basePath, Does.Not.EndWith("/"));
        Assert.That($"{basePath}/v1", Is.EqualTo(endpoint.TrimEnd('/')));
    }

    static void InitializeEndpoints()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new LiveContentApiEndpoints(),
            new SchemaRegistryApiEndpoints(),
        ]);
    }
}
