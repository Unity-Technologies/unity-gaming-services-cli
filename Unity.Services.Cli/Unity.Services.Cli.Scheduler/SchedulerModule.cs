using System.CommandLine;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RestSharp;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.Cli.Scheduler.Fetch;
using Unity.Services.Cli.Scheduler.Handlers;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Api;
using Unity.Services.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Scheduler.Authoring.Core.Serialization;
using Unity.Services.Scheduler.Authoring.Core.Service;
using FileSystem = Unity.Services.Cli.Scheduler.IO.FileSystem;
using IFileSystem = Unity.Services.Scheduler.Authoring.Core.IO.IFileSystem;

namespace Unity.Services.Cli.Scheduler;

/// <summary>
/// A Template module to achieve a get request command: ugs scheduler get `address` -o `file`
/// </summary>
public class SchedulerModule : ICommandModule
{
    class SchedulerInput : CommonInput
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

    public Command? ModuleRootCommand { get; }

    public SchedulerModule()
    {
        var schedulerListCommand = new Command("list", "List online schedules.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        schedulerListCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            ISchedulerClient,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            SchedulerListHandler.SchedulerListHandlerHandlerAsync);

        ModuleRootCommand = new("scheduler", "Scheduler module root command.")
        {
            ModuleRootCommand.AddNewFileCommand<ScheduleConfigFile>("Schedule"),
            schedulerListCommand
        };
        ModuleRootCommand.AddAlias("sched");
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(IServiceCollection serviceCollection)
    {
        var config = new Gateway.SchedulerApiV1.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<SchedulerEndpoints>()
        };
        config.DefaultHeaders.SetXClientIdHeader();

        serviceCollection.AddSingleton<ISchedulerClient, SchedulerClient>();
        serviceCollection.AddSingleton<ISchedulerApiAsync, SchedulerApi>(_ => new SchedulerApi(config));

        serviceCollection.AddTransient<IDeploymentService, SchedulerDeploymentService>();
        serviceCollection.AddTransient<IScheduleDeploymentHandler, SchedulerDeploymentHandler>();
        serviceCollection.AddTransient<IFetchService, SchedulerFetchService>();
        serviceCollection.AddTransient<IScheduleFetchHandler, SchedulerFetchHandler>();

        serviceCollection.AddTransient<IFileSystem, FileSystem>();
        serviceCollection.AddTransient<IScheduleResourceLoader, ScheduleResourceLoader>();
        serviceCollection.AddSingleton<ISchedulesSerializer, SchedulesSerializer>();

        var retryAfterPolicy = Policy
            .HandleResult<RestResponse>(r => r.StatusCode == HttpStatusCode.TooManyRequests && r.Headers != null)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: RetryAfterSleepDuration,
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
        Gateway.SchedulerApiV1.Generated.Client.RetryConfiguration.AsyncRetryPolicy = retryAfterPolicy;
    }

    static TimeSpan RetryAfterSleepDuration(int retryCount, DelegateResult<RestResponse> result, Context ctx)
    {
        const string retryAfter = "Retry-After";
        var header = result.Result.Headers!.First(x => x.Name!.Equals(retryAfter));
        var retryValue = header.Value?.ToString();
        var retryValueInt = int.Parse(retryValue!);
        var time = 2 * retryValueInt;
        return TimeSpan.FromSeconds(time);
    }
}
