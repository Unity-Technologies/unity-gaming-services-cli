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

        ExportCommand = new Command("export", "Export leaderboard configs.")
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

        ImportCommand = new Command("import", "Import leaderboard configs.")
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
            GetLeaderboardHandler.GetLeaderboardConfigAsync);
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
            DeleteLeaderboardCommand,
            GetLeaderboardCommand,
            ListLeaderboardsCommand,
            ResetLeaderboardCommand,
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
            BasePath = EndpointHelper.GetCurrentEndpointFor<LeaderboardEndpoints>()
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

