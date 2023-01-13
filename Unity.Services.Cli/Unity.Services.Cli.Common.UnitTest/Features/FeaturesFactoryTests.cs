using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Features;

namespace Unity.Services.Cli.Common.UnitTest.Features;

public class FeaturesFactoryTests
{
    readonly Mock<IFeatureManager> m_MockFeatureManager;
    readonly Mock<IHostBuilder> m_MockHostBuilder;

    public FeaturesFactoryTests()
    {
        var host = new Mock<IHost>();
        m_MockHostBuilder = new Mock<IHostBuilder>();
        m_MockHostBuilder.Setup(h => h.ConfigureAppConfiguration(It.IsAny<Action<HostBuilderContext, IConfigurationBuilder>>()))
            .Returns(m_MockHostBuilder.Object);
        m_MockHostBuilder.Setup(h => h.ConfigureServices(It.IsAny<Action<HostBuilderContext, IServiceCollection>>()))
            .Returns(m_MockHostBuilder.Object);
        m_MockHostBuilder.Setup(h => h.Build())
            .Returns(host.Object);

        m_MockFeatureManager = new Mock<IFeatureManager>(MockBehavior.Strict);

        var collection = new ServiceCollection();
        collection.AddSingleton(m_MockFeatureManager.Object);

        host.SetupGet(m => m.Services).Returns(collection.BuildServiceProvider());
    }

    [Test]
    public async Task BuildAsync_ReturnsFeatures()
    {
        m_MockFeatureManager.Setup(m => m.GetFeatureNamesAsync())
            .Returns(ToAsyncEnumerable(new List<string>()));

        var features = await FeaturesFactory.BuildAsync(m_MockHostBuilder.Object);

        Assert.IsNotNull(features);
    }

    [Test]
    public async Task BuildAsync_PassesKnownFeatures()
    {
        var feature = "TestFeature";
        m_MockFeatureManager.Setup(m => m.GetFeatureNamesAsync())
            .Returns(ToAsyncEnumerable(new List<string>{ feature }));
        m_MockFeatureManager.Setup(m => m.IsEnabledAsync(feature))
            .ReturnsAsync(true);

        var features = await FeaturesFactory.BuildAsync(m_MockHostBuilder.Object);

        Assert.IsTrue(features.IsEnabled(feature));
    }

    static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> enumerable)
    {
        foreach(var item in enumerable)
        {
            yield return await Task.FromResult(item);
        }
    }
}
