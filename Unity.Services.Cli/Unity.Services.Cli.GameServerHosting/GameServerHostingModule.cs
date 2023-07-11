using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.GameServerHosting.Endpoints;
using Unity.Services.Cli.GameServerHosting.Handlers;
using Unity.Services.Cli.GameServerHosting.Input;
using Unity.Services.Cli.GameServerHosting.Service;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Multiplay.Authoring.Core;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.Deployment;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using GameServerHostingConfiguration = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client.Configuration;
using CloudContentDeliveryConfiguration =
    Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Client.Configuration;
using IBuildsApi = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api.IBuildsApi;

namespace Unity.Services.Cli.GameServerHosting;

public class GameServerHostingModule : ICommandModule
{
    public GameServerHostingModule()
    {
        BuildCreateCommand = new Command("create", "Create a Game Server Hosting build.")
        {
            BuildCreateInput.BuildNameOption,
            BuildCreateInput.BuildOsFamilyOption,
            BuildCreateInput.BuildTypeOption,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildCreateCommand.SetHandler<
            BuildCreateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger, ILoadingIndicator,
            CancellationToken
        >(BuildCreateHandler.BuildCreateAsync);

        BuildCreateVersionCommand = new Command("create-version", "Create a new version of a Game Server Hosting build.")
        {
            BuildCreateVersionInput.BuildIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            BuildCreateVersionInput.AccessKeyOption,
            BuildCreateVersionInput.BucketUrlOption,
            BuildCreateVersionInput.ContainerTagOption,
            BuildCreateVersionInput.FileDirectoryOption,
            BuildCreateVersionInput.SecretKeyOption,
            BuildCreateVersionInput.RemoveOldFilesOption
        };
        BuildCreateVersionCommand.SetHandler<
            BuildCreateVersionInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            HttpClient,
            ILoadingIndicator,
            CancellationToken
        >(BuildCreateVersionHandler.BuildCreateVersionAsync);


        BuildDeleteCommand = new Command("delete", "Delete a Game Server Hosting build")
        {
            BuildIdInput.BuildIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildDeleteCommand.SetHandler<
            BuildIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(BuildDeleteHandler.BuildDeleteAsync);

        BuildGetCommand = new Command("get", "Get a Game Server Hosting build.")
        {
            BuildIdInput.BuildIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildGetCommand.SetHandler<
            BuildIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger, ILoadingIndicator,
            CancellationToken
        >(BuildGetHandler.BuildGetAsync);

        BuildInstallsCommand = new Command("installs", "List Game Server Hosting build installs")
        {
            BuildIdInput.BuildIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildInstallsCommand.SetHandler<
            BuildIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(BuildInstallsHandler.BuildInstallsAsync);

        BuildListCommand = new Command("list", "List Game Server Hosting builds.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildListCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger, ILoadingIndicator,
            CancellationToken
        >(BuildListHandler.BuildListAsync);

        BuildUpdateCommand = new Command("update", "Update a Game Server Hosting build")
        {
            BuildIdInput.BuildIdArgument,
            BuildUpdateInput.BuildNameOption,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildUpdateCommand.SetHandler<
            BuildUpdateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger, ILoadingIndicator,
            CancellationToken
        >(BuildUpdateHandler.BuildUpdateAsync);

        BuildCommand = new Command("build", "Manage Game Server Hosting builds.")
        {
            BuildCreateCommand,
            BuildCreateVersionCommand,
            BuildDeleteCommand,
            BuildGetCommand,
            BuildInstallsCommand,
            BuildListCommand,
            BuildUpdateCommand
        };

        BuildConfigurationGetCommand = new Command("get", "Get a Game Server Hosting build configurations.")
        {
            BuildConfigurationIdInput.BuildConfigurationIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildConfigurationGetCommand.SetHandler<
            BuildConfigurationIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(BuildConfigurationGetHandler.BuildConfigurationGetAsync);

        BuildConfigurationCreateCommand = new Command("create", "Create a Game Server Hosting build configuration.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            BuildConfigurationCreateInput.BinaryPathOption,
            BuildConfigurationCreateInput.BuildIdOption,
            BuildConfigurationCreateInput.CommandLineOption,
            BuildConfigurationCreateInput.ConfigurationOption,
            BuildConfigurationCreateInput.CoresOption,
            BuildConfigurationCreateInput.MemoryOption,
            BuildConfigurationCreateInput.NameOption,
            BuildConfigurationCreateInput.QueryTypeOption,
            BuildConfigurationCreateInput.SpeedOption,
        };
        BuildConfigurationCreateCommand.SetHandler<
            BuildConfigurationCreateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(BuildConfigurationCreateHandler.BuildConfigurationCreateAsync);

        BuildConfigurationDeleteCommand = new Command("delete", "Delete a Game Server Hosting build configurations.")
        {
            BuildConfigurationIdInput.BuildConfigurationIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        BuildConfigurationDeleteCommand.SetHandler<
            BuildConfigurationIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(BuildConfigurationDeleteHandler.BuildConfigurationDeleteAsync);

        BuildConfigurationListCommand = new Command("list", "List Game Server Hosting build configurations.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            BuildConfigurationListInput.FleetIdOption,
            BuildConfigurationListInput.PartialOption
        };
        BuildConfigurationListCommand.SetHandler<
            BuildConfigurationListInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(BuildConfigurationListHandler.BuildConfigurationListAsync);

        BuildConfigurationUpdateCommand = new Command("update", "Update a Game Server Hosting build configuration.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            BuildConfigurationUpdateInput.BuildConfigIdArgument,
            BuildConfigurationUpdateInput.BinaryPathOption,
            BuildConfigurationUpdateInput.BuildIdOption,
            BuildConfigurationUpdateInput.CommandLineOption,
            BuildConfigurationUpdateInput.ConfigurationOption,
            BuildConfigurationUpdateInput.CoresOption,
            BuildConfigurationUpdateInput.MemoryOption,
            BuildConfigurationUpdateInput.NameOption,
            BuildConfigurationUpdateInput.QueryTypeOption,
            BuildConfigurationUpdateInput.SpeedOption,
        };
        BuildConfigurationUpdateCommand.SetHandler<
            BuildConfigurationUpdateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(BuildConfigurationUpdateHandler.BuildConfigurationUpdateAsync);

        BuildConfigurationCommand = new Command("build-configuration", "Manage Game Server Hosting build configurations.")
        {
            BuildConfigurationGetCommand,
            BuildConfigurationCreateCommand,
            BuildConfigurationDeleteCommand,
            BuildConfigurationListCommand,
            BuildConfigurationUpdateCommand,
        };

        FleetCreateCommand = new Command("create", "Create Game Server Hosting fleet.")
        {
            FleetCreateInput.FleetNameOption,
            FleetCreateInput.FleetOsFamilyOption,
            FleetCreateInput.FleetRegionsOption,
            FleetCreateInput.FleetBuildConfigurationsOption,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        FleetCreateCommand.SetHandler<
            FleetCreateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(FleetCreateHandler.FleetCreateAsync);


        FleetDeleteCommand = new Command("delete", "Delete a Game Server Hosting fleet.")
        {
            FleetIdInput.FleetIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        FleetDeleteCommand.SetHandler<
            FleetIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(FleetDeleteHandler.FleetDeleteAsync);

        FleetGetCommand = new Command("get", "Get a Game Server Hosting fleet.")
        {
            FleetIdInput.FleetIdArgument,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        FleetGetCommand.SetHandler<
            FleetIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(FleetGetHandler.FleetGetAsync);

        FleetListCommand = new Command("list", "List Game Server Hosting fleets.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        FleetListCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(FleetListHandler.FleetListAsync);

        FleetUpdateCommand = new Command("update", "Update a Game Server Hosting fleet.")
        {
            FleetIdInput.FleetIdArgument,
            FleetUpdateInput.FleetNameOption,
            FleetUpdateInput.AllocTtlOption,
            FleetUpdateInput.DeleteTtlOption,
            FleetUpdateInput.DisabledDeleteTtlOption,
            FleetUpdateInput.ShutdownTtlOption,
            FleetUpdateInput.BuildConfigsOption,
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        FleetUpdateCommand.SetHandler<
            FleetUpdateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(FleetUpdateHandler.FleetUpdateAsync);

        FleetCommand = new Command("fleet", "Manage Game Server Hosting fleets.")
        {
            FleetCreateCommand,
            FleetDeleteCommand,
            FleetGetCommand,
            FleetListCommand,
            FleetUpdateCommand
        };


        FleetRegionTemplatesCommand = new Command("templates", "List Game Server Hosting templates for creating fleet regions.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption
        };
        FleetRegionTemplatesCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(RegionTemplatesHandler.RegionTemplatesAsync);

        FleetRegionAvailableCommand = new Command("available", "List Game Server Hosting available template regions for creating fleet regions.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            FleetIdInput.FleetIdArgument
        };
        FleetRegionAvailableCommand.SetHandler<
            FleetIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(RegionAvailableHandler.RegionAvailableAsync);

        FleetRegionCreateCommand = new Command("create", "Create Game Server Hosting fleet regions.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            FleetRegionCreateInput.FleetIdOption,
            FleetRegionCreateInput.RegionIdOption,
            FleetRegionCreateInput.MinAvailableServersOption,
            FleetRegionCreateInput.MaxServersOption
        };
        FleetRegionCreateCommand.SetHandler<
            FleetRegionCreateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(FleetRegionCreateHandler.FleetRegionCreateAsync);

        FleetRegionUpdateCommand = new Command("update", "Update Game Server Hosting fleet region.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            FleetRegionUpdateInput.FleetIdOption,
            FleetRegionUpdateInput.RegionIdOption,
            FleetRegionUpdateInput.MinAvailableServersOption,
            FleetRegionUpdateInput.MaxServersOption
        };

        FleetRegionUpdateCommand.SetHandler<
            FleetRegionUpdateInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(FleetRegionUpdateHandler.FleetRegionUpdateAsync);

        FleetRegionCommand = new Command("fleet-region", "Manage Game Server Hosting fleet regions.")
        {
            FleetRegionTemplatesCommand,
            FleetRegionAvailableCommand,
            FleetRegionCreateCommand,
            FleetRegionUpdateCommand
        };

        ServerGetCommand = new Command("get", "Get a Game Server Hosting server.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            ServerIdInput.ServerIdArgument
        };
        ServerGetCommand.SetHandler<
            ServerIdInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(ServerGetHandler.ServerGetAsync);

        ServerListCommand = new Command("list", "List Game Server Hosting servers.")
        {
            CommonInput.EnvironmentNameOption,
            CommonInput.CloudProjectIdOption,
            ServerListInput.FleetIdOption,
            ServerListInput.BuildConfigurationIdOption,
            ServerListInput.PartialOption,
            ServerListInput.StatusOption
        };
        ServerListCommand.SetHandler<
            ServerListInput,
            IUnityEnvironment,
            IGameServerHostingService,
            ILogger,
            ILoadingIndicator,
            CancellationToken
        >(ServerListHandler.ServerListAsync);

        ServerCommand = new Command("server", "Manage Game Server Hosting servers.")
        {
            ServerGetCommand,
            ServerListCommand
        };

        ModuleRootCommand = new Command("game-server-hosting", "Manage Game Sever Hosting resources.")
        {
            BuildCommand,
            BuildConfigurationCommand,
            FleetCommand,
            FleetRegionCommand,
            ServerCommand

        };

        ModuleRootCommand.AddAlias("gsh");
        BuildCommand.AddAlias("b");
        BuildConfigurationCommand.AddAlias("bc");
        FleetCommand.AddAlias("f");
        FleetRegionCommand.AddAlias("fr");
        ServerCommand.AddAlias("s");
    }

    internal Command BuildCommand { get; }
    internal Command BuildConfigurationCommand { get; }
    internal Command FleetCommand { get; }
    internal Command FleetRegionCommand { get; }
    internal Command ServerCommand { get; }

    // Build Commands
    internal Command BuildCreateCommand { get; }
    internal Command BuildCreateVersionCommand { get; }
    internal Command BuildDeleteCommand { get; }
    internal Command BuildGetCommand { get; }
    internal Command BuildInstallsCommand { get; }
    internal Command BuildListCommand { get; }
    internal Command BuildUpdateCommand { get; }

    // Build Configuration Commands
    internal Command BuildConfigurationGetCommand { get; }
    internal Command BuildConfigurationCreateCommand { get; }
    internal Command BuildConfigurationListCommand { get; }
    internal Command BuildConfigurationUpdateCommand { get; }
    internal Command BuildConfigurationDeleteCommand { get; }

    // Fleet Commands
    internal Command FleetCreateCommand { get; }
    internal Command FleetDeleteCommand { get; }
    internal Command FleetGetCommand { get; }
    internal Command FleetListCommand { get; }
    internal Command FleetUpdateCommand { get; }

    // Fleet Region Commands
    internal Command FleetRegionAvailableCommand { get; }
    internal Command FleetRegionTemplatesCommand { get; }
    internal Command FleetRegionCreateCommand { get; }
    internal Command FleetRegionUpdateCommand { get; }

    // Server Commands
    internal Command ServerGetCommand { get; }
    internal Command ServerListCommand { get; }


    internal static ExceptionFactory ExceptionFactory => (method, response) =>
    {
        var message = (string text) => $"Error calling {method}: {text}";

        // Handle errors from the backend
        var statusCode = (int)response.StatusCode;
        if (statusCode >= 400)
        {
            return new ApiException(
                statusCode,
                message(response.RawContent),
                response.RawContent,
                response.Headers
            );
        }

        // Handle errors from the client, such as serialization errors
        // These errors were being discarded before, but we want to surface them
        if (!string.IsNullOrEmpty(response.ErrorText))
        {
            return new ApiException(
                statusCode,
                message(response.ErrorText),
                response.Content,
                response.Headers
            );
        }

        return null!;
    };

    // GSH Module Command
    public Command ModuleRootCommand { get; }

    public static void RegisterServices(HostBuilderContext _, IServiceCollection serviceCollection)
    {
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var authenticationService = serviceProvider.GetRequiredService<IServiceAccountAuthenticationService>();

        var gameServerHostingConfiguration = new GameServerHostingConfiguration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<UnityServicesGatewayEndpoints>()
        };
        gameServerHostingConfiguration.DefaultHeaders.SetXClientIdHeader();

        IBuildsApi buildsApi = new BuildsApi(gameServerHostingConfiguration)
        {
            ExceptionFactory = ExceptionFactory
        };
        IBuildConfigurationsApi buildConfigurationsApi = new BuildConfigurationsApi(gameServerHostingConfiguration)
        {
            ExceptionFactory = ExceptionFactory
        };
        IFleetsApi fleetsApi = new FleetsApi(gameServerHostingConfiguration)
        {
            ExceptionFactory = ExceptionFactory
        };
        IServersApi serversApi = new ServersApi(gameServerHostingConfiguration)
        {
            ExceptionFactory = ExceptionFactory
        };

        GameServerHostingService service = new(
            authenticationService,
            buildsApi,
            buildConfigurationsApi,
            fleetsApi,
            serversApi
        );

        serviceCollection.AddSingleton<IGameServerHostingService>(service);

        serviceCollection.AddTransient<IFile>(_ => new FileSystem().File);
        serviceCollection.AddTransient<IDirectory>(_ => new FileSystem().Directory);

        RegisterApiClients(serviceCollection);
        RegisterAuthoringServices(serviceCollection);
    }

    static void RegisterApiClients(IServiceCollection serviceCollection)
    {
        var gameServerHostingConfig = new GameServerHostingConfiguration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<UnityServicesGatewayEndpoints>()
        };
        gameServerHostingConfig.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddSingleton<IBuildsApiAsync>(new BuildsApi(gameServerHostingConfig));
        serviceCollection.AddSingleton<IBuildConfigurationsApiAsync>(
            new BuildConfigurationsApi(gameServerHostingConfig));
        serviceCollection.AddSingleton<IFleetsApiAsync>(new FleetsApi(gameServerHostingConfig));

        var cloudContentDeliveryConfiguration = new CloudContentDeliveryConfiguration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<CloudContentDeliveryEndpoints>()
        };
        cloudContentDeliveryConfiguration.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddSingleton<IBucketsApiAsync>(new BucketsApi(cloudContentDeliveryConfiguration));
        serviceCollection.AddSingleton<IEntriesApiAsync>(new EntriesApi(cloudContentDeliveryConfiguration));
    }

    static void RegisterAuthoringServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<GameServerHostingApiConfig>();
        serviceCollection.AddScoped<IBuildsApiFactory, ApiClientFactory>();
        serviceCollection.AddScoped<IBuildConfigApiFactory, ApiClientFactory>();
        serviceCollection.AddScoped<IFleetApiFactory, ApiClientFactory>();
        serviceCollection.AddScoped<ICloudStorageFactory, ApiClientFactory>();

        serviceCollection.AddScoped<IMultiplayConfigValidator, MultiplayConfigValidator>();
        serviceCollection.AddScoped<MultiplayDeployer>();
        serviceCollection.AddScoped<IDeploymentFacadeFactory, DeploymentFacadeFactory>();
        serviceCollection.AddScoped<IDeploymentFacade, DeploymentFacade>();
        serviceCollection.AddScoped<IMultiplayBuildAuthoring, MultiplayBuildAuthoring>();
        serviceCollection.AddScoped<IBinaryBuilder, DummyBinaryBuilder>();
        serviceCollection.AddScoped<IBuildFileManagement, BuildFileManagement>();
        serviceCollection.AddScoped<IFileReader, FileReaderAdapter>();

        serviceCollection.AddScoped<IGameServerHostingConfigLoader, GameServerHostingConfigLoader>();
        serviceCollection.AddTransient<IDeployFileService, DeployFileService>();
    }
}
