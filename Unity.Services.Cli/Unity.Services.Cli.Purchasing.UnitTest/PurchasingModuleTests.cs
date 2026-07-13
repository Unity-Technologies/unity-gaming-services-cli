using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Purchasing.Input;
using Unity.Services.Cli.Purchasing.IO;
using Unity.Services.Cli.ServiceAccountAuthentication;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

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
    [TestCase(typeof(ICatalogLoader))]
    [TestCase(typeof(ICatalogCsvParser))]
    [TestCase(typeof(CliCsvCatalogLoader))]
    public void RegisterServices_RegistersExpectedServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new LiveContentApiEndpoints()
        ]);

        var collection = new ServiceCollection();
        collection.AddSingleton(
            new Mock<IServiceAccountAuthenticationService>().Object);

        PurchasingModule.RegisterServices(collection);

        Assert.That(
            collection.Any(d => d.ServiceType == serviceType),
            Is.True,
            $"{serviceType.Name} was not registered");
    }
}
