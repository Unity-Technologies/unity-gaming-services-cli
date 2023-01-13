using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unity.Services.Cli.Common.Services;

namespace Unity.Services.Cli.Common.UnitTest.Services;

public class ServiceTypesBridgeTests
{
    [Test]
    public void CreateBuilder_ReturnsCollection()
    {
        var bridge = new ServiceTypesBridge();

        var original = new ServiceCollection();
        var collection = bridge.CreateBuilder(original);

        Assert.AreSame(original, collection);
    }

    [Test]
    public void ServiceTypes_BeforeCreateServiceProvider_ThrowsException()
    {
        var bridge = new ServiceTypesBridge();

        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = bridge.ServiceTypes;
        });
    }

    [Test]
    public void ServiceTypes_ReturnsKnownTypes()
    {
        var bridge = new ServiceTypesBridge();

        var collection = bridge.CreateBuilder(new ServiceCollection());
        collection.AddScoped<object>();
        bridge.CreateServiceProvider(collection);

        Assert.Contains(typeof(object), bridge.ServiceTypes.ToList());
    }
}
