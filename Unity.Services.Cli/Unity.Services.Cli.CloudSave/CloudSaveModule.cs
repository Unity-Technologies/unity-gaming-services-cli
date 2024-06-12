using System.CommandLine;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RestSharp;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.CloudSave.Handlers;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.CloudSave.IO;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.CloudSave.Authoring.Core.Fetch;
using Unity.Services.CloudSave.Authoring.Core.IO;
using Unity.Services.CloudSave.Authoring.Core.Service;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Api;
using FileSystem = Unity.Services.Cli.CloudSave.IO.FileSystem;
using IFileSystem = Unity.Services.CloudSave.Authoring.Core.IO.IFileSystem;

namespace Unity.Services.Cli.CloudSave;

/// <summary>
/// A Template module to achieve a get request command: ugs cloudsave get `address` -o `file`
/// </summary>
public class CloudSaveModule : ICommandModule
{
    class CloudSaveInput : CommonInput
    {
        public static readonly Argument<string> AddressArgument = new(
            "address",
            "The address to send GET request");

        [InputBinding(nameof(AddressArgument))]
        public string? Address { get; set; }

        public static readonly Option<string> OutputFileOption = new(new[]
        {
            "-o",
            "--output"
        }, "Write output to file instead of stdout");

        [InputBinding(nameof(OutputFileOption))]
        public string? OutputFile { get; set; }
    }

    public Command ModuleRootCommand { get; }
    public Command ListIndexesCommand { get; }
    public Command QueryPlayerDataCommand { get; }
    public Command QueryCustomDataCommand { get; }
    public Command CreatePlayerIndexCommand { get; }
    public Command CreateCustomIndexCommand { get; }
    public Command ListCustomDataIdsCommand { get; }
    public Command ListPlayerDataIdsCommand { get; }

    public CloudSaveModule()
    {
        ListIndexesCommand = new Command("list", "List all indexes.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        ListIndexesCommand
            .SetHandler<
                CommonInput,
                IUnityEnvironment,
                ICloudSaveDataService,
                ILogger,
                ILoadingIndicator,
                CancellationToken>(
                ListIndexesHandler.ListIndexesAsync);

        QueryPlayerDataCommand = new Command("query", "Query player data.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            QueryDataInput.JsonFileOrBodyOption,
            QueryDataInput.VisibilityOption
        };
        QueryPlayerDataCommand
            .SetHandler<
                QueryDataInput,
                IUnityEnvironment,
                ICloudSaveDataService,
                ILogger,
                ILoadingIndicator,
                CancellationToken>(
                QueryPlayerDataHandler.QueryPlayerDataAsync);

        QueryCustomDataCommand = new Command("query", "Query custom entity data.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            QueryDataInput.JsonFileOrBodyOption,
            QueryDataInput.VisibilityOption
        };
        QueryCustomDataCommand
            .SetHandler<
                QueryDataInput,
                IUnityEnvironment,
                ICloudSaveDataService,
                ILogger,
                ILoadingIndicator,
                CancellationToken>(
                QueryCustomDataHandler.QueryCustomDataAsync);

        CreateCustomIndexCommand = new Command("create", "Create a custom index.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CreateIndexInput.JsonFileOrBodyOption,
            CreateIndexInput.FieldsOption,
            CreateIndexInput.VisibilityOption
        };
        CreateCustomIndexCommand
            .SetHandler<
                CreateIndexInput,
                IUnityEnvironment,
                ICloudSaveDataService,
                ILogger,
                ILoadingIndicator,
                CancellationToken>(
                CreateCustomIndexHandler.CreateCustomIndexAsync);

        CreatePlayerIndexCommand = new Command("create", "Create a new player data index.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            CreateIndexInput.JsonFileOrBodyOption,
            CreateIndexInput.FieldsOption,
            CreateIndexInput.VisibilityOption
        };
        CreatePlayerIndexCommand
            .SetHandler<
                CreateIndexInput,
                IUnityEnvironment,
                ICloudSaveDataService,
                ILogger,
                ILoadingIndicator,
                CancellationToken>(
                CreatePlayerIndexHandler.CreatePlayerIndexAsync);

        ListCustomDataIdsCommand = new Command("list", "Get a paginated list of all Game State custom data IDs for a given project and environment.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ListDataIdsInput.StartOption,
            ListDataIdsInput.LimitOption
        };
        ListCustomDataIdsCommand
            .SetHandler<
                ListDataIdsInput,
                IUnityEnvironment,
                ICloudSaveDataService,
                ILogger,
                ILoadingIndicator,
                CancellationToken>(
                ListCustomDataIdsHandler.ListCustomDataIdsAsync);

        ListPlayerDataIdsCommand = new Command("list", "Get a paginated list of all player data IDs for a given project and environment.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ListDataIdsInput.StartOption,
            ListDataIdsInput.LimitOption
        };
        ListPlayerDataIdsCommand
            .SetHandler<
                ListDataIdsInput,
                IUnityEnvironment,
                ICloudSaveDataService,
                ILogger,
                ILoadingIndicator,
                CancellationToken>(
                ListPlayerDataIdsHandler.ListPlayerDataIdsAsync);

        var indexPlayerCommand = new Command("player", "Create player indexes.")
        {
            CreatePlayerIndexCommand,
        };

        var indexCustomCommand = new Command("custom", "Create custom indexes.")
        {
            CreateCustomIndexCommand,
        };

        var indexCommand = new Command("index", "Create, list, or edit indexes.")
        {
            ListIndexesCommand,
            indexPlayerCommand,
            indexCustomCommand,
        };

        var playerCommand = new Command("player", "Query player data.")
        {
            QueryPlayerDataCommand,
            ListPlayerDataIdsCommand
        };

        var customCommand = new Command("custom", "Query custom entity data.")
        {
            QueryCustomDataCommand,
            ListCustomDataIdsCommand
        };

        // APIs
        var dataCommand = new Command("data", "Data API for Cloud Save.")
        {
            indexCommand,
            playerCommand,
            customCommand,
        };

        // Root
        ModuleRootCommand = new("cloud-save", "Manage Cloud Save indexes.")
        {
            dataCommand
        };
        ModuleRootCommand.AddAlias("cs");
    }

    /// <summary>
    /// Handler for get request command to handle operations
    /// </summary>
    /// <param name="input">
    /// CloudSave input automatically parsed. So developer does not need to retrieve from ParseResult.
    /// </param>
    /// <param name="client">
    /// The operation service for you command.
    /// </param>
    /// <param name="logger">
    /// A singleton logger to log output for commands.
    /// </param>
    /// <param name="loadingIndicator">A loading indicator to give user better feedback when some operation is taking time </param>
    /// <param name="fs">File system abstraction</param>
    /// <param name="cancellationToken">
    /// A cancellation token that should be propagated as much as possible to allow the command operations to be cancelled at any time.
    /// </param>
    static async Task GetAsync(
        CloudSaveInput input,
        ICloudSaveClient client,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        IFileSystem fs,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Sending Get Request...",
            context => GetAsync(input, client, logger, fs, cancellationToken));
    }

    static async Task GetAsync(
        CloudSaveInput input,
        ICloudSaveClient client,
        ILogger logger,
        IFileSystem fs,
        CancellationToken cancellationToken)
    {
        //TODO: This is a sample request for bootstrapping purposes
        var result = await client.RawGetRequest(input.Address, cancellationToken);

        //Information log will be hidden with `--quiet` option.
        logger.LogInformation("GET request succeed");

        if (string.IsNullOrEmpty(input.OutputFile))
        {
            // LogResultValue is to log a single result from service operation.
            // It will be parsed to json format with `--json` option
            logger.LogResultValue(result);
        }
        else
        {
            await fs.WriteAllText(input.OutputFile, result, cancellationToken);
        }
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(IServiceCollection serviceCollection)
    {
        var config = new Gateway.CloudSaveApiV1.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<CloudSaveEndpoints>()
        };
        config.DefaultHeaders.SetXClientIdHeader();
        AsyncPolicy<RestResponse> retryAfterPolicy = Policy
            .HandleResult<RestResponse>(r => r.StatusCode == HttpStatusCode.TooManyRequests && r.Headers != null)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: RetryAfterSleepDuration,
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
        Gateway.LeaderboardApiV1.Generated.Client.RetryConfiguration.AsyncRetryPolicy = retryAfterPolicy;
        serviceCollection.AddSingleton<IDataApiAsync>(new DataApi(config));
        serviceCollection.AddSingleton<ICloudSaveDataService, CloudSaveDataService>();

        // Registers services required for Deployment/Fetch
        // Register the command handler
        //serviceCollection.AddTransient<IDeploymentService, CloudSaveDeploymentService>();
        //serviceCollection.AddTransient<ICloudSaveDeploymentHandler, CloudSaveDeploymentHandler>();
        serviceCollection.AddTransient<ICloudSaveClient, Deploy.CloudSaveClient>();
        //serviceCollection.AddTransient<IFetchService, CloudSaveFetchService>();
        serviceCollection.AddTransient<ICloudSaveFetchHandler, CloudSaveFetchHandler>();
        serviceCollection.AddTransient<IFileSystem, FileSystem>();
        serviceCollection.AddTransient<ICloudSaveSimpleResourceLoader, CloudSaveSimpleResourceLoader>();

        //Gateway.CloudSaveApiV1.Generated.Client.RetryConfiguration.RetryPolicy =
        //    RetryPolicy.GetHttpRetryPolicy();
        //Gateway.CloudSaveApiV1.Generated.Client.RetryConfiguration.AsyncRetryPolicy =
        //    RetryPolicy.GetAsyncHttpRetryPolicy();
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
}
