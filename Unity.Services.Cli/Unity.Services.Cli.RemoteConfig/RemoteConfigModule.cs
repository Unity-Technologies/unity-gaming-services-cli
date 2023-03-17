using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Templates;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Fetch;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Formatting;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.IO;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Networking;
using FileSystem = Unity.Services.Cli.RemoteConfig.Deploy.FileSystem;
using IFileSystem = Unity.Services.RemoteConfig.Editor.Authoring.Core.IO.IFileSystem;

namespace Unity.Services.Cli.RemoteConfig;

/// <summary>
/// A module to issue commands to the Unity Remote Config service
/// </summary>
public class RemoteConfigModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public RemoteConfigModule()
    {
        ModuleRootCommand = new Command(
            name: "remote-config",
            description: "Manage RemoteConfig.")
        {
            ModuleRootCommand.AddNewFileCommand<RemoteConfigTemplate>("Remote Config")
        };

        ModuleRootCommand.AddAlias("rc");
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
        serviceCollection.AddTransient<IFileSystem, FileSystem>();
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

        serviceCollection.AddTransient<IRemoteConfigFetchHandler, RemoteConfigFetchHandler>();
        serviceCollection.AddTransient<IDeploymentService, RemoteConfigDeploymentService>();
        serviceCollection.AddTransient<IFetchService, RemoteConfigFetchService>();
    }
}
