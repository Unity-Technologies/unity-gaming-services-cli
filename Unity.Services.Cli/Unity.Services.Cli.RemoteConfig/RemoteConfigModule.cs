using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Networking;

namespace Unity.Services.Cli.RemoteConfig;

/// <summary>
/// A module to issue commands to the Unity Remote Config service
/// </summary>
public class RemoteConfigModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public RemoteConfigModule()
    {
        ModuleRootCommand = null;
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IRemoteConfigService, RemoteConfigService>();
        serviceCollection.AddTransient<IConfigTypeDeriver, ConfigTypeDeriver>();
        serviceCollection.AddTransient<IRemoteConfigParser, RemoteConfigParser>();
        serviceCollection.AddTransient<IRemoteConfigValidator, RemoteConfigValidator>();
        serviceCollection.AddTransient<IIllegalEntryDetector, IllegalEntryDetector>();
        serviceCollection.AddTransient<IFormatValidator, FormatValidator>();
        serviceCollection.AddTransient<IConfigMerger, ConfigMerger>();
        serviceCollection.AddTransient<IJsonConverter, JsonConverter>();
        serviceCollection.AddTransient<IFileReader, FileReader>();
        serviceCollection.AddTransient<IRemoteConfigScriptsLoader, RemoteConfigScriptsLoader>();

        serviceCollection.AddSingleton<RemoteConfigClient>();
        serviceCollection.AddSingleton<ICliRemoteConfigClient>(s => s.GetRequiredService<RemoteConfigClient>());
        serviceCollection.AddSingleton<IRemoteConfigClient>(s => s.GetRequiredService<RemoteConfigClient>());

        serviceCollection.AddSingleton<CliRemoteConfigDeploymentHandler>();
        serviceCollection.AddSingleton<IRemoteConfigServicesWrapper>(
            s => new RemoteConfigServicesWrapper(
            s.GetRequiredService<CliRemoteConfigDeploymentHandler>(),
            s.GetRequiredService<ICliRemoteConfigClient>(),
            s.GetRequiredService<CliRemoteConfigDeploymentHandler>(),
            s.GetRequiredService<IDeployFileService>(),
            s.GetRequiredService<IRemoteConfigService>(),
            s.GetRequiredService<IRemoteConfigScriptsLoader>()));

        serviceCollection.AddTransient<IDeploymentService, RemoteConfigDeploymentService>();
    }
}