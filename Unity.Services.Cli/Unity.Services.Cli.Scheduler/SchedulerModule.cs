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
using Unity.Services.Cli.Scheduler.Authoring;
using Unity.Services.Cli.Scheduler.Deploy;
using Unity.Services.Cli.Scheduler.Fetch;
using Unity.Services.Cli.Scheduler.Handlers;
using Unity.Services.Gateway.SchedulerApiV1.Generated.Api;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Deploy;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Service;
using FileSystem = Unity.Services.Cli.Scheduler.IO.FileSystem;

namespace Unity.Services.Cli.Scheduler;

/// <summary>
/// A Template module to achieve a get request command: ugs scheduler get `address` -o `file`
/// </summary>
public class SchedulerModule : ICommandModule
{
    public class SchedulerInput : CommonInput
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
        var schedulerListCommand = new Command("list", new CommandDescription("List online schedules.")
            .WithReturn("List of schedule items with name, eventName, scheduleType, schedule, payloadVersion, and payload.")
            .Build())
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

        ModuleRootCommand = new Command("scheduler", new CommandDescription("Manage Scheduler.")
            .WithDocs("https://docs.unity.com/en-us/triggers/tutorials/schedule-events")
            .WithAdminApi("https://services.docs.unity.com/scheduler-admin/v1/")
            .Build())
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
            BasePath = EndpointHelper.GetCurrentEndpointFor<SchedulerEndpoints>(),
            UserAgent = RequestHeaderHelper.UserAgent,
        };
        config.DefaultHeaders.SetXClientIdHeader();

        serviceCollection.AddSingleton<ISchedulerClient, SchedulerClient>();
        serviceCollection.AddSingleton<ISchedulerApiAsync, SchedulerApi>(_ => new SchedulerApi(config));

        serviceCollection.AddTransient<IDeploymentService, SchedulerDeploymentService>();
        serviceCollection.AddTransient<ISchedulerDeploymentHandler, SchedulerDeploymentHandler>();
        serviceCollection.AddTransient<IFetchService, SchedulerFetchService>();
        serviceCollection.AddTransient<ISchedulerFetchHandler, SchedulerFetchHandler>();
        serviceCollection.AddTransient<Tooling.Editor.Scheduler.Authoring.Core.Logger.ILogger, SchedulerAuthoringLogger>();

        serviceCollection.AddTransient<IFileSystem, FileSystem>();
        serviceCollection.AddTransient<ISchedulerResourceLoader, SchedulerResourceLoader>();

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
