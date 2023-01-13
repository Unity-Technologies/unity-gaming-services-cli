using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Handlers;
using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Crypto;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Api;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Unity.Services.Cli.CloudCode;

public class CloudCodeModule : ICommandModule
{
    internal Command ListCommand { get; }
    internal Command DeleteCommand { get; }
    internal Command PublishCommand { get; }
    internal Command GetCommand { get; }
    internal Command CreateCommand { get; }
    internal Command UpdateCommand { get; }
    public Command ModuleRootCommand { get; }

    public CloudCodeModule()
    {
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

        ModuleRootCommand = new Command("cloud-code", "Manage Cloud-Code scripts.")
        {
            CreateCommand,
            DeleteCommand,
            GetCommand,
            ListCommand,
            PublishCommand,
            UpdateCommand
        };
        ModuleRootCommand.AddAlias("cc");
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<CloudCodeEndpoints>()
        };
        config.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddSingleton<ICloudCodeApiAsync>(new CloudCodeApi(config));

        serviceCollection.AddTransient<IConfigurationValidator, ConfigurationValidator>();
        serviceCollection.AddTransient<ICloudScriptParametersParser, CloudScriptParametersParser>();
        serviceCollection.AddTransient<ICloudCodeScriptParser, CloudCodeScriptParser>();
        serviceCollection.AddTransient<ICloudCodeService, CloudCodeService>();
        serviceCollection.AddTransient<ICloudCodeInputParser, CloudCodeInputParser>();

        serviceCollection.AddSingleton<CloudCodeClient>();
        serviceCollection.AddSingleton<ICliCloudCodeClient>(s => s.GetRequiredService<CloudCodeClient>());
        serviceCollection.AddSingleton<ICloudCodeClient>(s => s.GetRequiredService<CloudCodeClient>());

        serviceCollection.AddSingleton<IDeploymentAnalytics, NoopDeploymentAnalytics>();
        serviceCollection.AddSingleton<Unity.Services.CloudCode.Authoring.Editor.Core.Logging.ILogger, CloudCodeAuthoringLogger>();

        serviceCollection.AddSingleton<EnvironmentProvider>();
        serviceCollection.AddSingleton<ICliEnvironmentProvider>(s => s.GetRequiredService<EnvironmentProvider>());
        serviceCollection.AddSingleton<IEnvironmentProvider>(s => s.GetRequiredService<EnvironmentProvider>());

        serviceCollection.AddTransient<ICloudCodeScriptsLoader, CloudCodeScriptsLoader>();

        serviceCollection.AddSingleton<IHashComputer, HashComputer>();
        serviceCollection.AddSingleton<IScriptCache, JsScriptCache>();
        serviceCollection.AddSingleton<IPreDeployValidator, PreDeployValidator>();
        serviceCollection.AddSingleton<CliCloudCodeDeploymentHandler>();
        serviceCollection.AddSingleton<ICloudCodeServicesWrapper>(
            s => new CloudCodeServicesWrapper(
                s.GetRequiredService<ICloudCodeService>(),
                s.GetRequiredService<IDeployFileService>(),
                s.GetRequiredService<ICloudCodeScriptsLoader>(),
                s.GetRequiredService<ICloudCodeInputParser>(),
                s.GetRequiredService<ICliCloudCodeClient>(),
                s.GetRequiredService<CliCloudCodeDeploymentHandler>(),
                s.GetRequiredService<CliCloudCodeDeploymentHandler>(),
                s.GetRequiredService<ICliEnvironmentProvider>()));

        serviceCollection.AddTransient<IDeploymentService, CloudCodeDeploymentService>();
    }
}
