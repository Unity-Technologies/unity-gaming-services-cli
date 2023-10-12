using Unity.Services.Cli.Authoring.Compression;
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
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Json;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using FileSystem = Unity.Services.Cli.RemoteConfig.Deploy.FileSystem;
using IFileSystem = Unity.Services.RemoteConfig.Editor.Authoring.Core.IO.IFileSystem;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.RemoteConfig.Handlers.ExportImport;
using Unity.Services.Cli.Common.Console;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.RemoteConfig;

/// <summary>
/// A module to issue commands to the Unity Remote Config service
/// </summary>
public class RemoteConfigModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }
    internal Command ExportCommand { get; }
    internal Command ImportCommand { get; }

    public RemoteConfigModule()
    {
        ExportCommand = new Command(
            name: "export",
            description: "Export Environment in a file")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            ExportInput.OutputDirectoryArgument,
            DryRunInput.DryRunOption,
            ExportInput.FileNameArgument
        };
        ExportCommand.SetHandler<ExportInput, RemoteConfigExporter, ILoadingIndicator, CancellationToken>(ExportHandler.ExportAsync);

        ImportCommand = new Command(
            name: "import",
            description: "Import Environment from a file")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            ImportInput.InputDirectoryArgument,
            DryRunInput.DryRunOption,
            ImportInput.ReconcileOption,
            ImportInput.FileNameArgument
        };
        ImportCommand.SetHandler<ImportInput, RemoteConfigImporter, ILoadingIndicator, CancellationToken>(ImportHandler.ImportAsync);

        ModuleRootCommand = new Command(
            name: "remote-config",
            description: "Manage RemoteConfig.")
        {
            ModuleRootCommand.AddNewFileCommand<RemoteConfigTemplate>("Remote Config"),
            ExportCommand,
            ImportCommand
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
            s.GetRequiredService<IRemoteConfigService>(),
            s.GetRequiredService<IRemoteConfigScriptsLoader>()));

        serviceCollection.AddTransient<IRemoteConfigFetchHandler, CliRemoteConfigFetchHandler>();
        serviceCollection.AddTransient<IDeploymentService, RemoteConfigDeploymentService>();
        serviceCollection.AddTransient<IFetchService, RemoteConfigFetchService>();
        serviceCollection.AddTransient<RemoteConfigExporter, RemoteConfigExporter>();
        serviceCollection.AddTransient<RemoteConfigImporter, RemoteConfigImporter>();
        serviceCollection.AddTransient<IZipArchiver, ZipArchiver>();
        serviceCollection.AddTransient<System.IO.Abstractions.IFileSystem, System.IO.Abstractions.FileSystem>();
    }
}
