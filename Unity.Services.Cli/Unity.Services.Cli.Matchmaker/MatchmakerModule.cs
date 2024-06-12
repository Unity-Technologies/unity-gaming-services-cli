using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Matchmaker.Parser;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Api;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Matchmaker.Authoring.Core.Deploy;
using Unity.Services.Matchmaker.Authoring.Core.Fetch;
using Unity.Services.Matchmaker.Authoring.Core.IO;
using Unity.Services.Matchmaker.Authoring.Core.Parser;

namespace Unity.Services.Cli.Matchmaker;

/// <summary>
/// A Template module to achieve a get request command: ugs matchmaker get `address` -o `file`
/// </summary>
public class MatchmakerModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }

    public MatchmakerModule()
    {
        ModuleRootCommand = new Command("matchmaker", "Matchmaker module root command.")
        {
            ModuleRootCommand.AddNewFileCommand<QueueConfigTemplate>("matchmaker queue")
        };
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext context, IServiceCollection serviceCollection)
    {
        var config = new Gateway.MatchmakerAdminApiV3.Generated.Client.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<AdminApiTargetEndpoint>()
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
