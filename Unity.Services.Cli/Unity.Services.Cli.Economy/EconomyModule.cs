using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Economy.Handlers;
using Unity.Services.Cli.Economy.Input;
using Unity.Services.Cli.Economy.Service;
using Unity.Services.Gateway.EconomyApiV2.Generated.Api;
using Unity.Services.Gateway.EconomyApiV2.Generated.Client;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Economy.Authoring;
using Unity.Services.Cli.Economy.Authoring.Deploy;
using Unity.Services.Cli.Economy.Authoring.Fetch;
using Unity.Services.Cli.Economy.Authoring.IO;
using Unity.Services.Cli.Economy.Templates;
using Unity.Services.Economy.Editor.Authoring.Core.Fetch;
using Unity.Services.Economy.Editor.Authoring.Core.Deploy;
using Unity.Services.Economy.Editor.Authoring.Core.IO;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Economy.Editor.Authoring.Core.Service;
using IMicrosoftLogger = Microsoft.Extensions.Logging.ILogger;
using ILogger = Unity.Services.Economy.Editor.Authoring.Core.Logging.ILogger;

namespace Unity.Services.Cli.Economy;

public class EconomyModule : ICommandModule
{
    public Command ModuleRootCommand { get; }
    internal Command ListCommand { get; }
    internal Command PublishCommand { get; }
    internal Command DeleteCommand { get; }
    internal Command CurrencyCommand { get; }
    internal Command InventoryItemCommand { get; }
    internal Command VirtualPurchaseCommand { get; }
    internal Command RealMoneyPurchaseCommand { get; }

    public EconomyModule()
    {
        ListCommand = new Command(
            "list",
            new CommandDescription("List Economy resources. Returns published resources by default; use --include-draft for draft resources.")
                .WithReturn("JSON array of resources (currencies, inventory items, virtual/real-money purchases) with id, name, type, type-specific fields, customData, and timestamps.")
                .Build())
        {
            EconomyInput.IncludeDraftOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption
        };
        ListCommand.AddAlias("l");
        ListCommand.SetHandler<EconomyInput, IUnityEnvironment, IEconomyService, IMicrosoftLogger, ILoadingIndicator, CancellationToken>(ListHandler.ListAsync);

        PublishCommand = new Command(
            "publish",
            new CommandDescription("Publish your Economy configuration.")
                .WithReturn("Confirmation message.")
                .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption
        };
        PublishCommand.SetHandler<CommonInput, IUnityEnvironment, IEconomyService, IMicrosoftLogger, ILoadingIndicator, CancellationToken>(PublishHandler.PublishAsync);

        DeleteCommand = new Command(
            "delete",
            new CommandDescription("Delete an Economy resource.")
                .WithReturn("Confirmation message.")
                .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            EconomyInput.ResourceIdArgument
        };
        DeleteCommand.SetHandler<EconomyInput, IUnityEnvironment, IEconomyService, IMicrosoftLogger, ILoadingIndicator, CancellationToken>(DeleteHandler.DeleteAsync);

        InventoryItemCommand = new Command("inventory", "Manage inventory configuration")
        {
            InventoryItemCommand.AddNewFileCommand<EconomyInventoryItemFile>(Constants.ServiceType, EconomyResourceTypes.InventoryItem)
        };
        InventoryItemCommand.AddAlias("i");
        CurrencyCommand = new Command("currency", "Manage currency configuration")
        {
            InventoryItemCommand.AddNewFileCommand<EconomyCurrencyFile>(Constants.ServiceType, EconomyResourceTypes.Currency)
        };
        CurrencyCommand.AddAlias("c");
        VirtualPurchaseCommand = new Command("virtual-purchase", "Manage virtual purchase configuration")
        {
            InventoryItemCommand.AddNewFileCommand<EconomyVirtualPurchaseFile>(Constants.ServiceType, EconomyResourceTypes.VirtualPurchase)
        };
        VirtualPurchaseCommand.AddAlias("vp");
        RealMoneyPurchaseCommand = new Command("real-money-purchase", "Manage real money purchase configuration")
        {
            InventoryItemCommand.AddNewFileCommand<EconomyRealMoneyPurchaseFile>(Constants.ServiceType, EconomyResourceTypes.MoneyPurchase)
        };
        RealMoneyPurchaseCommand.AddAlias("rmp");

        ModuleRootCommand = new("economy", new CommandDescription("Manage your Economy configuration.")
            .WithDocs("https://docs.unity.com/ugs/manual/economy/manual")
            .WithAdminApi("https://services.docs.unity.com/economy-admin/v2/")
            .Build())
        {
            DeleteCommand,
            ListCommand,
            PublishCommand,
            InventoryItemCommand,
            CurrencyCommand,
            VirtualPurchaseCommand,
            RealMoneyPurchaseCommand
        };

        ModuleRootCommand.AddAlias("ec");
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<UnityServicesGatewayEndpoints>()
        };
        config.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddSingleton<IEconomyAdminApiAsync>(new EconomyAdminApi(config));

        serviceCollection.AddTransient<IConfigurationValidator, ConfigurationValidator>();
        serviceCollection.AddTransient<IEconomyService, EconomyService>();

        serviceCollection.AddSingleton<ILogger, EconomyAuthoringLogger>();
        // Deploy

        serviceCollection.AddTransient<IEconomyJsonConverter, EconomyJsonConverter>();
        serviceCollection.AddTransient<IFileSystem, FileSystem>();

        serviceCollection.AddTransient<IEconomyResourcesLoader, EconomyResourcesLoader>();
        serviceCollection.AddSingleton<EconomyClient>();
        serviceCollection.AddSingleton<IEconomyClient>(s => s.GetRequiredService<EconomyClient>());
        serviceCollection.AddSingleton<ICliEconomyClient>(s => s.GetRequiredService<EconomyClient>());

        serviceCollection.AddSingleton<CliEconomyDeploymentHandler>(
            s => new CliEconomyDeploymentHandler(
                s.GetRequiredService<IEconomyClient>(),
                s.GetRequiredService<ILogger>()
            ));

        serviceCollection.AddSingleton<IEconomyDeploymentHandler>(s => s.GetRequiredService<CliEconomyDeploymentHandler>());

        serviceCollection.AddTransient<IDeploymentService, EconomyDeploymentService>();

        serviceCollection.AddSingleton<EconomyFetchHandler>(
            s => new EconomyFetchHandler(
                s.GetRequiredService<IEconomyClient>(),
                s.GetRequiredService<IEconomyResourcesLoader>(),
                s.GetRequiredService<IFileSystem>()
            ));

        serviceCollection.AddSingleton<IEconomyFetchHandler>(s => s.GetRequiredService<EconomyFetchHandler>());

        serviceCollection.AddTransient<IFetchService, EconomyFetchService>();
    }
}
