using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Authoring.UnitTest;

[TestFixture]
public class DeployModuleTests
{
    static readonly DeployModule k_DeployModule = new();

    [Test]
    public void DeployCommandHasCorrectArgument()
    {
        Assert.IsTrue(k_DeployModule!.ModuleRootCommand!.Arguments.Contains(DeployInput.PathsArgument));
    }

    [TestCase(typeof(IFile))]
    [TestCase(typeof(IDirectory))]
    [TestCase(typeof(IDeployFileService))]
    public void DeployModuleRegistersServices(Type serviceType)
    {
        var services = new List<ServiceDescriptor>();
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(DeployModule.RegisterServices);
        Assert.That(services.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }
}
