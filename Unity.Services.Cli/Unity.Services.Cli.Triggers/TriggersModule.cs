using System.CommandLine;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RestSharp;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Cli.Triggers.Fetch;
using Unity.Services.Cli.Triggers.Handlers;
using Unity.Services.Cli.Triggers.Input;
using Unity.Services.Cli.Triggers.IO;
using Unity.Services.Cli.Triggers.Service;
using Unity.Services.Gateway.TriggersApiV1.Generated.Api;
using Unity.Services.Triggers.Authoring.Core.Deploy;
using Unity.Services.Triggers.Authoring.Core.Fetch;
using Unity.Services.Triggers.Authoring.Core.Serialization;
using Unity.Services.Triggers.Authoring.Core.Service;
using FileSystem = Unity.Services.Cli.Triggers.IO.FileSystem;
using IFileSystem = Unity.Services.Triggers.Authoring.Core.IO.IFileSystem;

namespace Unity.Services.Cli.Triggers;

public class TriggersModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    static readonly Command k_ListCommand = new("list", new CommandDescription("List trigger configurations.")
        .WithReturn("JSON array of triggers with id, name, eventType, actionType, actionUrn, and timestamps.")
        .Build())
    {
        ListTriggersInput.LimitOption,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_GetCommand = new("get", new CommandDescription("Get a trigger configuration by ID.")
        .WithReturn("JSON trigger object with id, name, eventType, actionType, actionUrn, filter, and optional webhook config.")
        .Build())
    {
        GetTriggerInput.TriggerIdArgument,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DeleteCommand = new("delete", new CommandDescription("Delete a trigger configuration.")
        .WithReturn("Confirmation message.")
        .Build())
    {
        DeleteTriggerInput.TriggerIdArgument,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DlqListCommand = new("list", new CommandDescription("List failed events in the Dead Letter Queue.")
        .WithReturn("JSON array of DLQ event objects.")
        .Build())
    {
        ListDlqEventsInput.LimitOption,
        ListDlqEventsInput.StatusOption,
        ListDlqEventsInput.CreatedFromOption,
        ListDlqEventsInput.CreatedToOption,
        ListDlqEventsInput.ResolutionActionOption,
        ListDlqEventsInput.EventIdOption,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DlqGetCommand = new("get", new CommandDescription("Get a DLQ event by ID.")
        .WithReturn("JSON DLQ event object.")
        .Build())
    {
        DlqEventInput.EventIdArgument,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DlqReplayCommand = new("replay", new CommandDescription("Queue a failed DLQ event for replay.")
        .WithReturn("Confirmation message.")
        .Build())
    {
        DlqEventInput.EventIdArgument,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DlqDiscardCommand = new("discard", new CommandDescription("Discard a DLQ event without reprocessing.")
        .WithReturn("Confirmation message.")
        .Build())
    {
        DlqEventInput.EventIdArgument,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DlqReplayAllCommand = new("replay-all", new CommandDescription("Queue all pending DLQ events for replay.")
        .WithReturn("JSON with count of events queued.")
        .Build())
    {
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DlqDiscardAllCommand = new("discard-all", new CommandDescription("Discard all pending DLQ events.")
        .WithReturn("JSON with count of events discarded.")
        .Build())
    {
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption,
    };

    static readonly Command k_DlqCommand = new("dlq", "Dead Letter Queue management for failed trigger events.")
    {
        k_DlqListCommand,
        k_DlqGetCommand,
        k_DlqReplayCommand,
        k_DlqDiscardCommand,
        k_DlqReplayAllCommand,
        k_DlqDiscardAllCommand,
    };

    public TriggersModule()
    {
        k_ListCommand.SetHandler<
            ListTriggersInput,
            IUnityEnvironment,
            ITriggersService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(ListTriggersHandler.ListAsync);

        k_GetCommand.SetHandler<
            GetTriggerInput,
            IUnityEnvironment,
            ITriggersService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(GetTriggerHandler.GetAsync);

        k_DeleteCommand.SetHandler<
            DeleteTriggerInput,
            IUnityEnvironment,
            ITriggersService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(DeleteTriggersHandler.DeleteAsync);

        k_DlqListCommand.SetHandler<
            ListDlqEventsInput,
            IUnityEnvironment,
            IDlqService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(ListDlqEventsHandler.ListAsync);

        k_DlqGetCommand.SetHandler<
            DlqEventInput,
            IUnityEnvironment,
            IDlqService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(GetDlqEventHandler.GetAsync);

        k_DlqReplayCommand.SetHandler<
            DlqEventInput,
            IUnityEnvironment,
            IDlqService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(ReplayDlqEventHandler.ReplayAsync);

        k_DlqDiscardCommand.SetHandler<
            DlqEventInput,
            IUnityEnvironment,
            IDlqService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(DiscardDlqEventHandler.DiscardAsync);

        k_DlqReplayAllCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IDlqService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(ReplayAllDlqEventsHandler.ReplayAllAsync);

        k_DlqDiscardAllCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IDlqService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(DiscardAllDlqEventsHandler.DiscardAllAsync);

        ModuleRootCommand = new("triggers", new CommandDescription("Manage Triggers.")
            .WithDocs("https://docs.unity.com/en-us/triggers/tutorials/define-triggers/rest-api")
            .WithAdminApi("https://services.docs.unity.com/triggers-admin/v1/")
            .Build())
        {
            ModuleRootCommand.AddNewFileCommand<TriggersConfigFile>("Trigger"),
            k_ListCommand,
            k_GetCommand,
            k_DeleteCommand,
            k_DlqCommand,
        };

        ModuleRootCommand.AddAlias("tr");
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(IServiceCollection serviceCollection)
    {
        var config = new Gateway.TriggersApiV1.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<TriggersEndpoints>(),
            UserAgent = RequestHeaderHelper.UserAgent,
        };
        config.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddTransient<ITriggersSerializer, TriggersSerializer>();
        serviceCollection.AddTransient<ITriggersApiAsync, TriggersApi>(_ => new TriggersApi(config));
        serviceCollection.AddTransient<IDLQApiAsync, DLQApi>(_ => new DLQApi(config));
        serviceCollection.AddSingleton<ITriggersService, TriggersService>();
        serviceCollection.AddSingleton<IDlqService, DlqService>();
        serviceCollection.AddSingleton<ITriggersClient, TriggersClient>();
        // Registers services required for Deployment/Fetch
        // Register the command handler
        serviceCollection.AddTransient<IDeploymentService, TriggersDeploymentService>();
        serviceCollection.AddTransient<ITriggersDeploymentHandler, TriggersDeploymentHandler>();
        serviceCollection.AddTransient<IFetchService, TriggersFetchService>();
        serviceCollection.AddTransient<ITriggersFetchHandler, TriggersFetchHandler>();
        serviceCollection.AddTransient<IFileSystem, FileSystem>();
        serviceCollection.AddTransient<ITriggersResourceLoader, TriggersResourceLoader>();

        var retryAfterPolicy = Policy
            .HandleResult<RestResponse>(r => r.StatusCode == HttpStatusCode.TooManyRequests && r.Headers != null)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: RetryAfterSleepDuration,
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
        Gateway.TriggersApiV1.Generated.Client.RetryConfiguration.AsyncRetryPolicy = retryAfterPolicy;
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
