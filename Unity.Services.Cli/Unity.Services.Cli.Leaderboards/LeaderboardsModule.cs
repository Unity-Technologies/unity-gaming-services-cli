using System.CommandLine;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using RestSharp;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Authoring.Service;
using System.IO.Abstractions;
using Unity.Services.Cli.Leaderboards.Handlers.ImportExport;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Service;

using Unity.Services.Cli.Leaderboards.Handlers;
using Unity.Services.Cli.Leaderboards.Input;
using Unity.Services.Cli.Leaderboards.Service;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Api;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.Fetch;
using Unity.Services.Leaderboards.Authoring.Core.Serialization;
using LeaderboardFile = Unity.Services.Cli.Leaderboards.Deploy.LeaderboardConfigFile;
using CoreIFileSystem = Unity.Services.Leaderboards.Authoring.Core.IO.IFileSystem;
using CoreFileSystem = Unity.Services.Cli.Leaderboards.IO.FileSystem;

namespace Unity.Services.Cli.Leaderboards;

public class LeaderboardsModule : ICommandModule
{
    internal Command ExportCommand { get; }
    internal Command ImportCommand { get; }
    internal Command ListLeaderboardsCommand { get; }
    internal Command GetLeaderboardCommand { get; }
    internal Command CreateLeaderboardCommand { get; }
    internal Command UpdateLeaderboardCommand { get; }
    internal Command DeleteLeaderboardCommand { get; }
    internal Command ResetLeaderboardCommand { get; }
    internal Command ScoresCommand { get; }
    internal Command BucketsCommand { get; }

    public Command ModuleRootCommand { get; }

    public LeaderboardsModule()
    {
        ListLeaderboardsCommand = new Command("list", new CommandDescription("List leaderboards.")
            .WithReturn("JSON array of leaderboard summaries with id and name.")
            .Build())
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

        ExportCommand = new Command("export", new CommandDescription("Export leaderboard configs.")
            .WithReturn("A zip file written to the output directory.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ExportInput.OutputDirectoryArgument,
            ExportInput.DryRunOption,
            ExportInput.FileNameArgument
        };
        ExportCommand.SetHandler<
            ExportInput,
            ILogger,
            LeaderboardExporter,
            ILoadingIndicator,
            CancellationToken>(ExportHandler.ExportAsync);

        ImportCommand = new Command("import", new CommandDescription("Import leaderboard configs.")
            .WithReturn("Per-item status messages indicating created, updated, or deleted.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ImportInput.InputDirectoryArgument,
            ImportInput.DryRunOption,
            ImportInput.ReconcileOption,
            ImportInput.FileNameArgument
        };
        ImportCommand.SetHandler<
            ImportInput,
            ILogger,
            LeaderboardImporter,
            ILoadingIndicator,
            CancellationToken>(
            ImportHandler.ImportAsync);

        GetLeaderboardCommand = new Command("get", new CommandDescription("Get detailed leaderboard info.")
            .WithReturn("Leaderboard config with id, name, sortOrder, updateType, bucketSize, resetConfig, tieringConfig, versions, and timestamps.")
            .Build())
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
            GetLeaderboardHandler.GetLeaderboardConfigAsync);
        CreateLeaderboardCommand = new Command("create", new CommandDescription("Create a new leaderboard.")
            .WithReturn("Confirmation message.")
            .Build())
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

        UpdateLeaderboardCommand = new Command("update", new CommandDescription("Update a leaderboard.")
            .WithReturn("Confirmation message.")
            .Build())
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

        DeleteLeaderboardCommand = new Command("delete", new CommandDescription("Delete a leaderboard.")
            .WithReturn("Confirmation message.")
            .Build())
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

        ResetLeaderboardCommand = new Command("reset", new CommandDescription("Reset a leaderboard.")
            .WithReturn("Confirmation message with optional archived version ID.")
            .Build())
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

        // Scores group
        var listScoresCommand = new Command("list", new CommandDescription("List scores for a leaderboard.")
            .WithReturn("JSON with offset, limit, total, and results array of {playerId, playerName, score, rank, tier}.")
            .Build())
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            PaginatedLeaderboardInput.TierOption,
            LeaderboardIdInput.VersionOption,
            PaginatedLeaderboardInput.OffsetOption,
            PaginatedLeaderboardInput.LimitOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        listScoresCommand.SetHandler<
            PaginatedLeaderboardInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListScoresHandler.ListScoresAsync);

        var getPlayerScoreCommand = new Command("get", new CommandDescription("Get a player's score on a leaderboard.")
            .WithReturn("Player score with updatedTime, bucketId, playerId, playerName, score, rank, and tier.")
            .Build())
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            PlayerScoreInput.PlayerIdArgument,
            LeaderboardIdInput.VersionOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        getPlayerScoreCommand.SetHandler<
            PlayerScoreInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetPlayerScoreHandler.GetPlayerScoreAsync);

        var getPlayerRangeCommand = new Command("get-range", new CommandDescription("Get scores around a player on a leaderboard.")
            .WithReturn("JSON with results array of {playerId, playerName, score, rank, tier}.")
            .Build())
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            PlayerScoreInput.PlayerIdArgument,
            LeaderboardIdInput.VersionOption,
            PlayerRangeInput.RangeLimitOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        getPlayerRangeCommand.SetHandler<
            PlayerRangeInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetPlayerRangeHandler.GetPlayerRangeAsync);

        var getScoresByPlayerIdsCommand = new Command("get-by-player-ids", new CommandDescription("Get scores for specific players on a leaderboard.")
            .WithReturn("JSON with results array of {playerId, playerName, score, rank, tier} and entriesNotFoundForPlayerIds.")
            .Build())
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            LeaderboardIdInput.VersionOption,
            PlayerIdsInput.PlayerIdsOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        getScoresByPlayerIdsCommand.SetHandler<
            PlayerIdsInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetScoresByPlayerIdsHandler.GetScoresByPlayerIdsAsync);

        var deletePlayerScoreCommand = new Command("delete", new CommandDescription("Delete a player's score from a leaderboard.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            PlayerScoreInput.PlayerIdArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        deletePlayerScoreCommand.SetHandler<
            PlayerScoreInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            DeletePlayerScoreHandler.DeletePlayerScoreAsync);

        var purgePlayerScoresCommand = new Command("purge", new CommandDescription("Purge a player's scores from all leaderboards.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            PurgePlayerInput.PlayerIdArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        purgePlayerScoresCommand.SetHandler<
            PurgePlayerInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            PurgePlayerScoresHandler.PurgePlayerScoresAsync);

        ScoresCommand = new Command("scores", "Manage leaderboard scores.")
        {
            listScoresCommand,
            getPlayerScoreCommand,
            getPlayerRangeCommand,
            getScoresByPlayerIdsCommand,
            deletePlayerScoreCommand,
            purgePlayerScoresCommand,
        };

        // Buckets group
        var listBucketsCommand = new Command("list", new CommandDescription("List buckets for a leaderboard.")
            .WithReturn("JSON with offset, limit, total, and results array of bucket IDs.")
            .Build())
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            LeaderboardIdInput.VersionOption,
            PaginatedLeaderboardInput.OffsetOption,
            PaginatedLeaderboardInput.LimitOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        listBucketsCommand.SetHandler<
            PaginatedLeaderboardInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListBucketsHandler.ListBucketsAsync);

        var getBucketScoresCommand = new Command("scores", new CommandDescription("List scores in a leaderboard bucket.")
            .WithReturn("JSON with offset, limit, total, and results array of {playerId, playerName, score, rank, tier}.")
            .Build())
        {
            LeaderboardIdInput.RequestLeaderboardIdArgument,
            BucketScoresInput.BucketIdArgument,
            PaginatedLeaderboardInput.TierOption,
            LeaderboardIdInput.VersionOption,
            PaginatedLeaderboardInput.OffsetOption,
            PaginatedLeaderboardInput.LimitOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        getBucketScoresCommand.SetHandler<
            BucketScoresInput,
            IUnityEnvironment,
            ILeaderboardsService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetBucketScoresHandler.GetBucketScoresAsync);

        BucketsCommand = new Command("buckets", "Manage leaderboard buckets.")
        {
            listBucketsCommand,
            getBucketScoresCommand,
        };

        ModuleRootCommand = new Command("leaderboards", new CommandDescription("Manage Leaderboards.")
            .WithDocs("https://docs.unity.com/ugs/manual/leaderboards/manual")
            .WithAdminApi("https://services.docs.unity.com/leaderboards-admin/v1/")
            .Build())
        {
            CreateLeaderboardCommand,
            UpdateLeaderboardCommand,
            DeleteLeaderboardCommand,
            GetLeaderboardCommand,
            ListLeaderboardsCommand,
            ResetLeaderboardCommand,
            ScoresCommand,
            BucketsCommand,
            ModuleRootCommand.AddNewFileCommand<LeaderboardFile>("Leaderboard"),
            ExportCommand,
            ImportCommand
        };
        ModuleRootCommand.AddAlias("lb");
    }

    public static TimeSpan RetryAfterSleepDuration(int retryCount, DelegateResult<RestResponse> result, Context ctx)
    {
        const string retryAfter = "Retry-After";
        var header = result.Result.Headers!.First(x => x.Name!.Equals(retryAfter));
        var retryValue = header.Value?.ToString();
        var retryValueInt = int.Parse(retryValue!);
        var time = 2 * retryValueInt;
        return TimeSpan.FromSeconds(time);
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Gateway.LeaderboardApiV1.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<LeaderboardEndpoints>(),
            UserAgent = RequestHeaderHelper.UserAgent,
        };
        config.DefaultHeaders.SetXClientIdHeader();
        AsyncPolicy<RestResponse> retryAfterPolicy = Policy
            .HandleResult<RestResponse>(r => r.StatusCode == HttpStatusCode.TooManyRequests && r.Headers != null)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: RetryAfterSleepDuration,
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
        Gateway.LeaderboardApiV1.Generated.Client.RetryConfiguration.AsyncRetryPolicy = retryAfterPolicy;
        serviceCollection.AddTransient<ILeaderboardsApiAsync>(_ => new LeaderboardsApi(config));
        serviceCollection.AddTransient<IConfigurationValidator, ConfigurationValidator>();
        serviceCollection.AddSingleton<ILeaderboardsService, LeaderboardsService>();
        serviceCollection.AddSingleton<ILeaderboardsClient, LeaderboardsClient>();
        serviceCollection.AddTransient<LeaderboardImporter, LeaderboardImporter>();
        serviceCollection.AddTransient<LeaderboardExporter, LeaderboardExporter>();
        serviceCollection.AddTransient<IFileSystem, FileSystem>();
        serviceCollection.AddTransient<CoreIFileSystem, CoreFileSystem>();
        serviceCollection.AddTransient<IDeploymentService, LeaderboardDeploymentService>();
        serviceCollection.AddTransient<IFetchService, LeaderboardFetchService>();
        serviceCollection.AddTransient<ILeaderboardsDeploymentHandler, LeaderboardsDeploymentHandler>();
        serviceCollection.AddTransient<ILeaderboardsFetchHandler, LeaderboardsFetchHandler>();
        serviceCollection.AddTransient<ILeaderboardsConfigLoader, LeaderboardsConfigLoader>();
        serviceCollection.AddTransient<ILeaderboardsSerializer, LeaderboardsSerializer>();
        serviceCollection.AddTransient<LeaderboardImporter, LeaderboardImporter>();
        serviceCollection.AddTransient<LeaderboardExporter, LeaderboardExporter>();
    }
}
