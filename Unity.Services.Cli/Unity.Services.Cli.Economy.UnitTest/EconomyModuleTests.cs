using System;
using System.Collections.Generic;
using System.CommandLine.Builder;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Economy.Service;
using Unity.Services.Cli.TestUtils;
using Unity.Services.Gateway.EconomyApiV2.Generated.Api;

namespace Unity.Services.Cli.Economy.UnitTest;

[TestFixture]
public class EconomyModuleTests
{
    static readonly EconomyModule k_EconomyModule = new();

    [Test]
    public void BuildCommands_CreateEconomyCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_EconomyModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, k_EconomyModule.ModuleRootCommand!.Name,
            out var resultCommand);
        Assert.AreEqual(k_EconomyModule.ModuleRootCommand, resultCommand);
        Assert.NotNull(k_EconomyModule.GetResourcesCommand!.Handler);
        Assert.NotNull(k_EconomyModule.GetPublishedCommand!.Handler);
        Assert.NotNull(k_EconomyModule.PublishCommand!.Handler);
        Assert.NotNull(k_EconomyModule.DeleteCommand!.Handler);
    }

    [Test]
    public void BuildCommands_CreateAliases()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_EconomyModule);

        Assert.IsTrue(k_EconomyModule.ModuleRootCommand!.Aliases.Contains("ec"));
    }

    [Test]
    public void Commands_ContainsRequiredInputs()
    {
        Assert.IsTrue(k_EconomyModule.GetResourcesCommand!.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_EconomyModule.GetResourcesCommand.Options.Contains(CommonInput.EnvironmentNameOption));

        Assert.IsTrue(k_EconomyModule.GetPublishedCommand!.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_EconomyModule.GetPublishedCommand.Options.Contains(CommonInput.EnvironmentNameOption));

        Assert.IsTrue(k_EconomyModule.PublishCommand!.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_EconomyModule.PublishCommand.Options.Contains(CommonInput.EnvironmentNameOption));

        Assert.IsTrue(k_EconomyModule.DeleteCommand!.Options.Contains(CommonInput.CloudProjectIdOption));
        Assert.IsTrue(k_EconomyModule.DeleteCommand.Options.Contains(CommonInput.EnvironmentNameOption));
    }

    [TestCase(typeof(IEconomyService))]
    [TestCase(typeof(IConfigurationValidator))]
    [TestCase(typeof(IEconomyAdminApiAsync))]
    public void ConfigureEconomyRegistersExpectedServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(UnityServicesGatewayEndpoints).GetTypeInfo()
        });
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(EconomyModule.RegisterServices);
        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
}
