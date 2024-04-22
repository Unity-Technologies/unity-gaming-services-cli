using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.CloudContentDelivery.Authoring.Core.IO;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;

namespace CloudContentDeliveryTest;

public class CloudContentDeliveryModuleTests
{
    static readonly CloudContentDeliveryModule k_Module = new();

    [Test]
    public void ValidateRootCommand()
    {
        Assert.That(k_Module.ModuleRootCommand, Is.Not.Null);
        Assert.Multiple(
            () =>
            {
                Assert.That(k_Module.ModuleRootCommand?.Name, Is.EqualTo("ccd"));
                Assert.That(k_Module.ModuleRootCommand?.Description, Is.EqualTo("Manage Cloud Content Delivery."));
            });
    }

    [Test]
    public void ConfigureCloudContentDeliveryRegistersExpectedServices()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
            new[]
            {
                typeof(CloudContentDeliveryApiEndpoints).GetTypeInfo()
            });

        var collection = new ServiceCollection();
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object));

        CloudContentDeliveryModule.RegisterServices(
            new HostBuilderContext(new Dictionary<object, object>()),
            collection);
        Assert.Multiple(
            () =>
            {
                Assert.That(collection.Any(descriptor => descriptor.ServiceType == typeof(IFileSystem)));
                Assert.That(collection.Any(descriptor => descriptor.ServiceType == typeof(IBadgesApi)));
                Assert.That(collection.Any(descriptor => descriptor.ServiceType == typeof(IBucketsApi)));
                Assert.That(collection.Any(descriptor => descriptor.ServiceType == typeof(IReleasesApi)));
                Assert.That(collection.Any(descriptor => descriptor.ServiceType == typeof(IEntriesApi)));
                Assert.That(collection.Any(descriptor => descriptor.ServiceType == typeof(IPermissionsApi)));
            });
    }

    [Test]
    public void CloudContentDeliveryRegisterModulesCommands()
    {
        var rootCommand = new Command("root", "Root Command");
        CloudContentDeliveryModule.RegisterModulesCommands(rootCommand);

        var commandNames = new[]
        {
            "buckets",
            "entries",
            "badges",
            "releases"
        };

        foreach (var commandName in commandNames)
            Assert.That(
                rootCommand.Children.Any(subcommand => subcommand.Name == commandName),
                Is.True,
                $"Command '{commandName}' should exist.");
    }

}
