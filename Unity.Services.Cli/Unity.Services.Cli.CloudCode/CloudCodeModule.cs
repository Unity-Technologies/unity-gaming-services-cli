using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Handlers;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Crypto;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Api;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.CloudCode.Handlers.ImportExport.Scripts;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.CloudCode.Handlers.ImportExport.Modules;
using Unity.Services.Cli.CloudCode.Templates;
using Unity.Services.Cli.CloudCode.Utils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Unity.Services.Cli.CloudCode;

public class CloudCodeModule : ICommandModule
{
    internal Command ExportScriptsCommand { get; }
    internal Command ImportScriptsCommand { get; }
    internal Command ListCommand { get; }
    internal Command DeleteCommand { get; }
    internal Command PublishCommand { get; }
    internal Command GetCommand { get; }
    internal Command CreateCommand { get; }
    internal Command UpdateCommand { get; }
    internal Command NewFileCommand { get; }
    internal Command ScriptsCommand { get; }
    public Command ModuleRootCommand { get; }

    public CloudCodeModule()
    {
        ExportScriptsCommand = new Command("export", "Export Cloud Code scripts.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ExportInput.OutputDirectoryArgument,
            ExportInput.DryRunOption,
            ExportInput.FileNameArgument
        };
        ExportScriptsCommand.SetHandler<
            ExportInput,
            CloudCodeScriptsExporter,
            ILoadingIndicator,
            CancellationToken>(ScriptExportHandler.ExportAsync);
        ImportScriptsCommand = new Command("import", "Import Cloude Code scripts.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ImportInput.InputDirectoryArgument,
            ImportInput.DryRunOption,
            ImportInput.ReconcileOption,
            ImportInput.FileNameArgument
        };
        ImportScriptsCommand.SetHandler<
            ImportInput,
            CloudCodeScriptsImporter,
            ILoadingIndicator,
            CancellationToken>(
            ImportHandler.ImportAsync);
        ListCommand = new Command("list", "List Cloud-Code scripts.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption
        };
        ListCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            ICloudCodeService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListHandler.ListAsync);

        PublishCommand = new Command("publish", "Publish Cloud-Code scripts.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CloudCodeInput.ScriptNameArgument,
            CloudCodeInput.VersionOption
        };
        PublishCommand.SetHandler<
            CloudCodeInput,
            IUnityEnvironment,
            ICloudCodeService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            PublishHandler.PublishAsync);

        DeleteCommand = new Command("delete", "Delete Cloud-Code scripts.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CloudCodeInput.ScriptNameArgument
        };
        DeleteCommand.SetHandler<
            CloudCodeInput,
            IUnityEnvironment,
            ICloudCodeService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            DeleteHandler.DeleteAsync);

        GetCommand = new Command("get", "Get a Cloud-Code script.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CloudCodeInput.ScriptNameArgument
        };
        GetCommand.SetHandler<
            CloudCodeInput,
            IUnityEnvironment,
            ICloudCodeService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetHandler.GetAsync);

        CreateCommand = new Command("create", "Create a Cloud-Code script.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CloudCodeInput.ScriptTypeOption,
            CloudCodeInput.ScriptLanguageOption,
            CloudCodeInput.ScriptNameArgument,
            CloudCodeInput.FilePathArgument
        };
        CreateCommand.SetHandler<
            CloudCodeInput,
            IUnityEnvironment,
            ICloudCodeService,
            ICloudCodeInputParser,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            CreateHandler.CreateAsync);

        UpdateCommand = new Command("update", "Update a Cloud-Code script.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CloudCodeInput.ScriptNameArgument,
            CloudCodeInput.FilePathArgument
        };
        UpdateCommand.SetHandler<
            CloudCodeInput,
            IUnityEnvironment,
            ICloudCodeService,
            ICloudCodeInputParser,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            UpdateHandler.UpdateAsync);

        NewFileCommand = ModuleRootCommand.AddNewFileCommand<CloudCodeTemplate>(CloudCodeConstants.ServiceType);

        ScriptsCommand = new Command(
            "scripts",
            "Manage Cloud-Code scripts.")
        {
            ListCommand,
            PublishCommand,
            DeleteCommand,
            GetCommand,
            CreateCommand,
            UpdateCommand,
            NewFileCommand,
            ExportScriptsCommand,
            ImportScriptsCommand
        };

        ScriptsCommand.AddAlias("s");

        ModuleRootCommand = new Command(
            "cloud-code",
            "Manage Cloud-Code scripts and modules.")
        {
            ScriptsCommand
        };

        ModuleRootCommand.AddAlias("cc");

        RegisterModulesCommands(ModuleRootCommand);
    }

    static void RegisterModulesCommands(Command root)
    {
        var getModuleCommand = new Command(
            "get",
            "Get a Cloud-Code module.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CloudCodeInput.ModuleNameArgument
        };
        getModuleCommand.SetHandler<
            CloudCodeInput,
            IUnityEnvironment,
            ICloudCodeService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetModuleHandler.GetModuleAsync);

        var deleteModuleCommand = new Command(
            "delete",
            "Delete a Cloud-Code module.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CloudCodeInput.ModuleNameArgument
        };
        deleteModuleCommand.SetHandler<CloudCodeInput, IUnityEnvironment, ICloudCodeService, ILogger, ILoadingIndicator, CancellationToken>(
            DeleteModuleHandler.DeleteModuleAsync);

        var listModuleCommand = new Command(
            "list",
            "List Cloud-Code modules.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption
        };
        listModuleCommand.SetHandler<CommonInput, IUnityEnvironment, ICloudCodeService, ILogger, ILoadingIndicator,
            CancellationToken>(ListModulesHandler.ListModulesAsync);

        var exportModulesCommand = new Command("export", "Export Cloud Code modules.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ExportInput.OutputDirectoryArgument,
            ExportInput.DryRunOption,
            ExportInput.FileNameArgument
        };
        exportModulesCommand.SetHandler<
            ExportInput,
            CloudCodeModulesExporter,
            ILoadingIndicator,
            CancellationToken>(ModuleExportHandler.ExportAsync);

        var importModulesCommand = new Command("import", "Import Cloud Code modules.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ImportInput.InputDirectoryArgument,
            ImportInput.DryRunOption,
            ImportInput.ReconcileOption,
            ImportInput.FileNameArgument
        };
        importModulesCommand.SetHandler<
            ImportInput,
            CloudCodeModulesImporter,
            ILoadingIndicator,
            CancellationToken>(
            ModulesImportHandler.ImportAsync);

        var modulesHandlerCommand = new Command(
            "modules",
            "Manage Cloud-Code modules.")
        {
            getModuleCommand,
            listModuleCommand,
            deleteModuleCommand,
            exportModulesCommand,
            importModulesCommand
        };

        modulesHandlerCommand.AddAlias("m");

        root.Add(modulesHandlerCommand);
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<CloudCodeEndpoints>(),
        };
        config.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddSingleton<ICloudCodeApiAsync>(new CloudCodeApi(config));
        serviceCollection.AddSingleton<ICSharpClient, CloudCodeModuleClient>();
        serviceCollection.AddSingleton<IJavaScriptClient, CloudCodeScriptClient>();
        serviceCollection.AddSingleton<IDeploymentAnalytics, NoopDeploymentAnalytics>();
        serviceCollection.AddSingleton<
            Unity.Services.CloudCode.Authoring.Editor.Core.Logging.ILogger, CloudCodeAuthoringLogger>();
        serviceCollection.AddSingleton<EnvironmentProvider>();
        serviceCollection.AddSingleton<ICliEnvironmentProvider>(s => s.GetRequiredService<EnvironmentProvider>());
        serviceCollection.AddSingleton<IEnvironmentProvider>(s => s.GetRequiredService<EnvironmentProvider>());
        serviceCollection.AddSingleton<IHashComputer, HashComputer>();
        serviceCollection.AddSingleton<IScriptCache, JsScriptCache>();
        serviceCollection.AddSingleton<IPreDeployValidator, PreDeployValidator>();
        serviceCollection.AddSingleton<ICloudCodeModulesLoader, CloudCodeModulesLoader>();
        serviceCollection.AddSingleton<HttpClient>();
        serviceCollection.AddSingleton<ICloudCodeModulesDownloader, CloudCodeModulesDownloader>();
        serviceCollection.AddSingleton<ICloudCodeScriptsLoader, CloudCodeScriptsLoader>();
        serviceCollection.AddSingleton<ICloudCodeService, CloudCodeService>();
        serviceCollection.AddSingleton<ICloudCodeInputParser, CloudCodeInputParser>();
        serviceCollection.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        serviceCollection.AddSingleton<ICloudScriptParametersParser, CloudScriptParametersParser>();
        serviceCollection.AddSingleton<ICloudCodeScriptParser, CloudCodeScriptParser>();
        serviceCollection.AddSingleton<CliCloudCodeDeploymentHandler<IJavaScriptClient>>();
        serviceCollection.AddSingleton<CliCloudCodeDeploymentHandler<ICSharpClient>>();
        serviceCollection.AddSingleton<IJavaScriptFetchHandler, JavaScriptFetchHandler>();
        serviceCollection.AddSingleton<IEqualityComparer<IScript>, CloudCodeScriptNameComparer>();

        serviceCollection.AddTransient<IDeploymentService, CloudCodeScriptDeploymentService>(CreateJavaScriptDeployService);
        serviceCollection.AddTransient<IDeploymentService, CloudCodePrecompiledModuleDeploymentService>(CreateCSharpDeployService);
        serviceCollection.AddTransient<IFetchService, JavaScriptFetchService>();

        serviceCollection.AddTransient<CloudCodeScriptsExporter, CloudCodeScriptsExporter>();
        serviceCollection.AddTransient<CloudCodeScriptsImporter, CloudCodeScriptsImporter>();
        serviceCollection.AddTransient<CloudCodeModulesExporter, CloudCodeModulesExporter>();
        serviceCollection.AddTransient<CloudCodeModulesImporter, CloudCodeModulesImporter>();

        serviceCollection.AddTransient<IZipArchiver, ZipArchiver>();
    }

    internal static CloudCodeScriptDeploymentService CreateJavaScriptDeployService(IServiceProvider provider)
    {
        return new CloudCodeScriptDeploymentService(
            provider.GetRequiredService<ICloudCodeInputParser>(),
            provider.GetRequiredService<ICloudCodeScriptParser>(),
            provider.GetRequiredService<CliCloudCodeDeploymentHandler<IJavaScriptClient>>(),
            provider.GetRequiredService<ICloudCodeScriptsLoader>(),
            provider.GetRequiredService<ICliEnvironmentProvider>(),
            provider.GetRequiredService<IJavaScriptClient>());
    }

    internal static CloudCodePrecompiledModuleDeploymentService CreateCSharpDeployService(IServiceProvider provider)
    {
        return new CloudCodePrecompiledModuleDeploymentService(
            provider.GetRequiredService<CliCloudCodeDeploymentHandler<ICSharpClient>>(),
            provider.GetRequiredService<ICloudCodeModulesLoader>(),
            provider.GetRequiredService<ICliEnvironmentProvider>(),
            provider.GetRequiredService<ICSharpClient>());
    }
}
