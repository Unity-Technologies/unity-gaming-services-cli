using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace Unity.Services.Cli.Common.Features;

public static class FeaturesFactory
{
    /// <param name="builder">New Builder to be consumed. Build will be called so it must not be shared.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<IFeatures> BuildAsync(IHostBuilder builder, CancellationToken cancellationToken = default)
    {
        if (ShouldEnableAllFeatureFlags())
        {
            return new AlwaysEnabledFeatures();
        }

        var services = builder.
            ConfigureAppConfiguration(CommonModule.ConfigAppConfiguration)
            .ConfigureServices((_, collection) =>
            {
                collection.AddFeatureManagement();
            }).Build();
        var featureManager = services.Services.GetRequiredService<IFeatureManager>();
        return new StaticFeatures(await LoadAllFeatureFlagsAsync(featureManager, cancellationToken));
    }

    static async Task<Dictionary<string, bool>> LoadAllFeatureFlagsAsync(IFeatureManager featureManager, CancellationToken cancellationToken = default)
    {
        var flags = new Dictionary<string, bool>();
        await foreach (var name in featureManager.GetFeatureNamesAsync().WithCancellation(cancellationToken))
        {
            flags[name] = await featureManager.IsEnabledAsync(name);
        }
        return flags;
    }

    static bool ShouldEnableAllFeatureFlags()
    {
#if ALL_FEATURE_FLAGS_ENABLED
        return true;
#else
        return false;
#endif
    }
}
