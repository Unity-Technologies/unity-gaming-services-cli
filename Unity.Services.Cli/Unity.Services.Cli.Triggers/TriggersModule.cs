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
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Triggers.Deploy;
using Unity.Services.Cli.Triggers.Fetch;
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

/// <summary>
/// A Template module to achieve a get request command: ugs triggers get `address` -o `file`
/// </summary>
public class TriggersModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public TriggersModule()
    {
        ModuleRootCommand = new("triggers", "Triggers module root command.")
        {
            ModuleRootCommand.AddNewFileCommand<TriggersConfigFile>("Trigger"),
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
            BasePath = EndpointHelper.GetCurrentEndpointFor<TriggersEndpoints>()
        };
        config.DefaultHeaders.SetXClientIdHeader();
        serviceCollection.AddTransient<ITriggersSerializer, TriggersSerializer>();
        serviceCollection.AddTransient<ITriggersApiAsync, TriggersApi>(_ => new TriggersApi(config));
        serviceCollection.AddSingleton<ITriggersService, TriggersService>();
        serviceCollection.AddSingleton<ITriggersClient, TriggersClient>();
        // Registers services required for Deployment/Fetch
        // Register the command handler
        serviceCollection.AddTransient<IDeploymentService, TriggersDeploymentService>();
        serviceCollection.AddTransient<ITriggersDeploymentHandler, TriggersDeploymentHandler>();

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
