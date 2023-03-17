using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Leaderboards.Handlers;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Api;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;


namespace Unity.Services.Cli.Leaderboards;

public class LeaderboardsModule : ICommandModule
{
    internal Command ListLeaderboardsCommand { get; }
    internal Command GetLeaderboardCommand { get; }
    internal Command CreateLeaderboardCommand { get; }
    internal Command UpdateLeaderboardCommand { get; }
    internal Command DeleteLeaderboardCommand { get; }
    internal Command ResetLeaderboardCommand { get; }

    public Command ModuleRootCommand { get; }

    public LeaderboardsModule()
    {
        ListLeaderboardsCommand = new Command("list", "List leaderboards.")
        {
            ListLeaderboardInput.CursorOption,
            ListLeaderboardInput.LimitOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        ListLeaderboardsCommand.SetHandler<
            ListLeaderboardInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetLeaderboardConfigsHandler.GetLeaderboardConfigsAsync);


        GetLeaderboardCommand = new Command("get", "Get detailed leaderboard info.")
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        GetLeaderboardCommand.SetHandler<
            LeaderboardIdInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetLeaderboardConfigHandler.GetLeaderboardConfigAsync);

        CreateLeaderboardCommand = new Command("create", "Create a new leaderboard.")
        {
            CreateInput.RequestBodyArgument,
        };
        CreateLeaderboardCommand.SetHandler<
            CreateInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(CreateLeaderboardHandler.CreateLeaderboardAsync);

        UpdateLeaderboardCommand = new Command("update", "Update a leaderboard.")
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            UpdateInput.RequestBodyArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        UpdateLeaderboardCommand.SetHandler<
            UpdateInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(UpdateLeaderboardHandler.UpdateLeaderboardAsync);

        DeleteLeaderboardCommand = new Command("delete", "Delete a leaderboard.")
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        DeleteLeaderboardCommand.SetHandler<
            LeaderboardIdInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(DeleteLeaderboardHandler.DeleteLeaderboardAsync);

        ResetLeaderboardCommand = new Command("reset", "Reset a leaderboard.")
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            ResetInput.ResetArchiveArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        ResetLeaderboardCommand.SetHandler<
            ResetInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(ResetLeaderboardHandler.ResetLeaderboardAsync);

        ModuleRootCommand = new Command("leaderboards", "Manage Leaderboards.")
        {
            CreateLeaderboardCommand,
            DeleteLeaderboardCommand,
            GetLeaderboardCommand,
            UpdateLeaderboardCommand,
            ListLeaderboardsCommand,
            ResetLeaderboardCommand,
        };
        ModuleRootCommand.AddAlias("lb");
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Gateway.LeaderboardApiV1.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<LeaderboardEndpoints>()
        };
        config.DefaultHeaders.SetXClientIdHeader();

        serviceCollection.AddTransient<ILeaderboardsApiAsync>(_ => new LeaderboardsApi(config));
        serviceCollection.AddTransient<IConfigurationValidator, ConfigurationValidator>();
        serviceCollection.AddTransient<IZipArchiver<UpdatedLeaderboardConfig>, ZipArchiver<UpdatedLeaderboardConfig>>();
        serviceCollection.AddSingleton<ILeaderboardsService, LeaderboardsService>();

    }
}
