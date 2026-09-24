using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Policies;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Observability.Handlers;
using Unity.Services.Cli.Observability.Input;
using Unity.Services.Cli.Observability.Service;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Api;

namespace Unity.Services.Cli.Observability;

public class ObservabilityModule : ICommandModule
{
    internal Command ListLogsCommand { get; }
    internal Command LogsCommand { get; }

    public Command ModuleRootCommand { get; }

    public ObservabilityModule()
    {
        ListLogsCommand = new Command("list", "List logs for a project and environment.")
        {
            ListLogsInput.FromOption,
            ListLogsInput.ToOption,
            ListLogsInput.QueryOption,
            ListLogsInput.OffsetOption,
            ListLogsInput.LimitOption,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        ListLogsCommand.SetHandler<
            ListLogsInput,
            IUnityEnvironment,
            IObservabilityService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetLogsHandler.GetLogsAsync);

        LogsCommand = new Command("logs", "Manage Observability logs.")
        {
            ListLogsCommand,
        };

        ModuleRootCommand = new Command("observability", "Query the Observability service. API docs: https://services.docs.unity.com/observability/v1/")
        {
            LogsCommand,
        };
        ModuleRootCommand.AddAlias("obs");
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Gateway.ObservabilityApiV1.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<ObservabilityEndpoints>(),
            UserAgent = RequestHeaderHelper.UserAgent,
        };
        config.DefaultHeaders.SetXClientIdHeader();
        Gateway.ObservabilityApiV1.Generated.Client.RetryConfiguration.AsyncRetryPolicy =
            RetryPolicy.GetAsyncHttpRetryPolicy();
        serviceCollection.AddTransient<ILogsApiAsync>(_ => new LogsApi(config));
        serviceCollection.AddTransient<IConfigurationValidator, ConfigurationValidator>();
        serviceCollection.AddSingleton<IObservabilityService, ObservabilityService>();
    }
}
