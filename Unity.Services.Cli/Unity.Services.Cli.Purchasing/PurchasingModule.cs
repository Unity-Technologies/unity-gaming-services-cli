using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Policies;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Purchasing.Authoring;
using Unity.Services.Cli.Purchasing.Handlers;
using Unity.Services.Cli.Purchasing.Input;
using Unity.Services.Cli.Purchasing.IO;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Api;
using Unity.Services.Gateway.LiveContentApiV1.Generated.Client;
using UnityEditor.Purchasing.Editor.Authoring.Core.Deploy;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using CoreLogger = UnityEditor.Purchasing.Editor.Authoring.Core.Logger;
using CoreFileSystem = UnityEditor.Purchasing.Editor.Authoring.Core.IO.IFileSystem;

namespace Unity.Services.Cli.Purchasing;

public class PurchasingModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public PurchasingModule()
    {
        var purchasingListCommand = new Command("list", new CommandDescription("List catalog items.")
            .WithReturn("Table of SKU, Product Type, Language, Title, Currency, Amount; or JSON array of catalog items with --json.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        purchasingListCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            ILiveContentConfigClient,
            IConsoleTable,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(PurchasingListHandler.PurchasingListHandlerHandlerAsync);

        var ucatTemplate = new CatalogItemConfigFile();
        var csvTemplate = new CatalogCsvConfigFile();

        var newFileCommand = new Command("new-file", "Create new purchasing config file.")
        {
            NewFileInput.FileArgument,
            CommonInput.UseForceOption,
            PurchasingNewFileInput.CsvOption,
        };

        var combinedExtension = $"{ucatTemplate.Extension} (default) | {csvTemplate.Extension} (--csv)";
        var combinedBody = $"{ucatTemplate.FileBodyText}\n\nWith --csv:\n{csvTemplate.FileBodyText}";
        FileTemplateRegistry.Register(newFileCommand, combinedExtension, combinedBody);

        newFileCommand.SetHandler<PurchasingNewFileInput, System.IO.Abstractions.IFile, ILogger, CancellationToken>(
            (input, file, logger, token) =>
            {
                IFileTemplate template = input.Csv ? csvTemplate : ucatTemplate;
                return NewFileHandler.NewFileAsync(input, file, template, logger, token);
            });

        ModuleRootCommand = new("purchasing", new CommandDescription("Manage In-App Purchasing catalog items.")
            .WithDocs("https://docs.unity.com/ugs/manual/iap/manual")
            .Build())
        {
            purchasingListCommand,
            newFileCommand,

        };
        ModuleRootCommand.AddAlias("iap");
    }

    public static void RegisterServices(IServiceCollection serviceCollection)
    {
        var config = new Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<LiveContentApiEndpoints>(),
            Timeout = 600000,
            UserAgent = "ugs_cli/1.0.0",
        };
        config.DefaultHeaders.SetXClientIdHeader();

        RetryConfiguration.RetryPolicy = RetryPolicy.GetHttpRetryPolicy();
        RetryConfiguration.AsyncRetryPolicy = RetryPolicy.GetAsyncHttpRetryPolicy();

        serviceCollection.AddSingleton<IConfigsApiAsync>(new ConfigsApi(config));

        serviceCollection.AddSingleton<CoreLogger.ILogger, PurchasingAuthoringLogger>();

        serviceCollection.AddSingleton<PurchasingClient>();
        serviceCollection.AddSingleton<ILiveContentConfigClient>(
            s => s.GetRequiredService<PurchasingClient>());

        serviceCollection.AddSingleton<CoreFileSystem, FileSystem>();
        serviceCollection.AddSingleton<ICatalogLoader, CliUcatCatalogLoader>();

        serviceCollection.AddSingleton<ICatalogCsvParser, CatalogCsvParser>();
        serviceCollection.AddSingleton<CliCsvCatalogLoader>();

        serviceCollection.AddSingleton<ICatalogCsvParser, CatalogCsvParser>();
        serviceCollection.AddSingleton<CliCsvCatalogLoader>();

        serviceCollection.AddSingleton<ICatalogDeploymentHandler>(s =>
            new CatalogDeploymentHandler(
                s.GetRequiredService<ILiveContentConfigClient>(),
                s.GetRequiredService<CoreLogger.ILogger>()));

        serviceCollection.AddTransient<IDeploymentService, PurchasingDeploymentService>();
        serviceCollection.AddTransient<IFetchService, PurchasingFetchService>();
    }
}
