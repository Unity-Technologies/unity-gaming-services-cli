using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Matchmaker.Handlers;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Api;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Deploy;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Fetch;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.IO;
using Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Parser;

namespace Unity.Services.Cli.Matchmaker;

/// <summary>
/// A Template module to achieve a get request command: ugs matchmaker get `address` -o `file`
/// </summary>
public class MatchmakerModule : ICommandModule
{
    internal Command EnvironmentConfigCommand { get; }
    internal Command QueueCommand { get; }
    internal Command RestrictionsCommand { get; }

    public Command? ModuleRootCommand { get; }

    public MatchmakerModule()
    {
        var getEnvironmentConfigCommand = new Command("get", new CommandDescription("Get the matchmaker environment configuration.")
            .WithReturn("Enabled and defaultQueueName.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        getEnvironmentConfigCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IMatchmakerService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetEnvironmentConfigHandler.GetEnvironmentConfigAsync);

        EnvironmentConfigCommand = new Command("environment-config", "Manage matchmaker environment configuration.")
        {
            getEnvironmentConfigCommand,
        };

        var listQueuesCommand = new Command("list", new CommandDescription("List all matchmaker queues.")
            .WithReturn("JSON array of queue summaries with name, enabled, and maxPlayersPerTicket.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        listQueuesCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IMatchmakerService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            ListQueuesHandler.ListQueuesAsync);

        QueueCommand = new Command("queue", "Manage matchmaker queues.")
        {
            listQueuesCommand,
        };

        var getRestrictionsCommand = new Command("get", new CommandDescription("Get matchmaker restrictions.")
            .WithReturn("Restriction limits including maxQueues, maxPoolsPerQueue, maxPlayersPerTicket, and other configuration limits.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };
        getRestrictionsCommand.SetHandler<
            CommonInput,
            IUnityEnvironment,
            IMatchmakerService,
            ILogger,
            ILoadingIndicator,
            CancellationToken>(
            GetRestrictionsHandler.GetRestrictionsAsync);

        RestrictionsCommand = new Command("restrictions", "Manage matchmaker restrictions.")
        {
            getRestrictionsCommand,
        };

        ModuleRootCommand = new Command("matchmaker", new CommandDescription("Manage Matchmaker.")
            .WithDocs("https://docs.unity.com/ugs/manual/matchmaker/manual")
            .WithAdminApi("https://services.docs.unity.com/matchmaker-admin/v3/")
            .Build())
        {
            ModuleRootCommand.AddNewFileCommand<QueueConfigTemplate>("matchmaker queue"),
            EnvironmentConfigCommand,
            QueueCommand,
            RestrictionsCommand,
        };
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext context, IServiceCollection serviceCollection)
    {
        var config = new Gateway.MatchmakerAdminApiV3.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<AdminApiTargetEndpoint>(),
            UserAgent = RequestHeaderHelper.UserAgent,
        };
        config.DefaultHeaders.SetXClientIdHeader();

        serviceCollection.AddSingleton<IMatchmakerAdminApi>(new MatchmakerAdminApi(config));
        serviceCollection.AddSingleton<IMatchmakerConfigParser, MatchmakerConfigParser>();
        serviceCollection.AddSingleton<IConfigApiClient, AdminApiClient.MatchmakerAdminClient>();
        serviceCollection.AddSingleton<IMatchmakerService, MatchmakerService>();
        serviceCollection.AddSingleton<IMatchmakerDeployHandler, MatchmakerDeployHandler>();
        serviceCollection.AddSingleton<IMatchmakerFetchHandler, MatchmakerFetchHandler>();
        serviceCollection.AddSingleton<IDeepEqualityComparer, MatchmakerConfigParser>();
        serviceCollection.AddSingleton<IFileSystem, FileSystem>();
        serviceCollection.AddSingleton<IDeploymentService, MatchmakerDeploymentService>();
        serviceCollection.AddSingleton<IFetchService, MatchmakerFetchService>();
    }
}
