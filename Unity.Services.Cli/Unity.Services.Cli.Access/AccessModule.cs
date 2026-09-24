using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Access.Handlers;
using Unity.Services.Cli.Access.Input;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Gateway.AccessApiV1.Generated.Api;
using Unity.Services.Gateway.AccessApiV1.Generated.Client;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Deploy;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Fetch;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Json;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Validations;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.IO;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Service;
using Unity.Services.Cli.Access.Deploy;
using Unity.Services.Cli.Access.IO;
using Unity.Services.Cli.Access.Models;
using Unity.Services.Cli.Authoring.Handlers;
using Unity.Services.Cli.Authoring.Service;

namespace Unity.Services.Cli.Access;

public class AccessModule : ICommandModule
{
    internal Command ProjectPolicyCommand;
    internal Command PlayerPolicyCommand;
    internal Command ProjectPolicyListCommand;
    internal Command ProjectPolicyDeleteCommand;
    internal Command PlayerPolicyListCommand;
    internal Command PlayerPolicyUpdateCommand;
    internal Command PlayerPolicyDeleteCommand;
    public Command ModuleRootCommand { get; }

    public AccessModule()
    {
        ProjectPolicyListCommand = new Command("list", new CommandDescription("Retrieves policies for a project and environment.")
            .WithReturn("JSON object with policy statements, each containing sid, action, effect, principal, and resource.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };

        ProjectPolicyListCommand
            .SetHandler<CommonInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator, CancellationToken>(
                ProjectPolicyListHandler.ListProjectPolicyAsync);

        ProjectPolicyDeleteCommand = new Command("delete", new CommandDescription("Delete statements from project policy by statement ID.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            AccessInput.StatementIdsArgument,
        };

        ProjectPolicyDeleteCommand
            .SetHandler<AccessInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator, CancellationToken>(
                ProjectPolicyDeleteHandler.DeleteProjectPolicyAsync);

        ProjectPolicyCommand = new Command("project-policy", "Manage resource policies for a project.")
        {
            ProjectPolicyListCommand,
            ProjectPolicyDeleteCommand,
            ProjectPolicyCommand.AddNewFileCommand<NewProjectAccessFile>("ProjectAccess"),
        };

        PlayerPolicyListCommand = new Command("list", new CommandDescription("Retrieves player policies for a project and environment. Returns all player policies when --player-id is omitted, or the policy for the specified player when --player-id is provided.")
            .WithReturn("JSON array of all player policies when no --player-id is specified, or a JSON object with playerId and statements when --player-id is provided.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            PlayerPolicyListInput.PlayerIdOption,
        };

        PlayerPolicyListCommand
            .SetHandler<PlayerPolicyListInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator, CancellationToken>(
                PlayerPolicyListHandler.ListPlayerPolicyAsync);

        PlayerPolicyUpdateCommand = new Command("update", new CommandDescription("Upsert statements in player policy.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            PlayerPolicyInput.PlayerIdArgument,
            AccessInput.FilePathArgument,
        };

        PlayerPolicyUpdateCommand
            .SetHandler<PlayerPolicyInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator, CancellationToken>(
                PlayerPolicyUpdateHandler.UpdatePlayerPolicyAsync);

        PlayerPolicyDeleteCommand = new Command("delete", new CommandDescription("Delete statements from player policy by statement ID.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            PlayerPolicyInput.PlayerIdArgument,
            AccessInput.StatementIdsArgument,
        };

        PlayerPolicyDeleteCommand
            .SetHandler<PlayerPolicyInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator, CancellationToken>(
                PlayerPolicyDeleteHandler.DeletePlayerPolicyAsync);

        PlayerPolicyCommand = new Command("player-policy", "Manage resource policies for players.")
        {
            PlayerPolicyListCommand,
            PlayerPolicyUpdateCommand,
            PlayerPolicyDeleteCommand,
        };

        ModuleRootCommand = new Command("access", new CommandDescription("Manage resource policies to restrict read/write access.")
            .WithDocs("https://docs.unity.com/en-us/services/access-control")
            .WithAdminApi("https://services.docs.unity.com/access/v1/")
            .Build())
        {
            ProjectPolicyCommand,
            PlayerPolicyCommand,
        };

        ModuleRootCommand.AddAlias("ac");
    }

    /// <summary>
    ///     Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var config = new Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<AccessEndpoints>(),
            UserAgent = RequestHeaderHelper.UserAgent,
        };
        config.DefaultHeaders.SetXClientIdHeader();

        // API Clients
        serviceCollection.AddSingleton<IProjectPolicyApi>(new ProjectPolicyApi(config));
        serviceCollection.AddSingleton<IPlayerPolicyApi>(new PlayerPolicyApi(config));

        serviceCollection.AddTransient<IProjectAccessParser, ProjectAccessParser>();
        serviceCollection.AddTransient<IProjectAccessConfigValidator, ProjectAccessConfigValidator>();
        serviceCollection.AddTransient<IProjectAccessMerger, ProjectAccessMerger>();
        serviceCollection.AddTransient<IFileSystem, FileSystem>();
        serviceCollection.AddTransient<IAccessConfigLoader, AccessConfigLoader>();
        serviceCollection.AddTransient<IJsonConverter, JsonConverter>();

        serviceCollection.AddSingleton<ProjectAccessClient>();
        serviceCollection.AddSingleton<IProjectAccessClient>(s => s.GetRequiredService<ProjectAccessClient>());


        serviceCollection.AddSingleton<IAccessService, AccessService>();

        serviceCollection.AddTransient<IDeploymentService, ProjectAccessDeploymentService>();
        serviceCollection.AddTransient<IProjectAccessDeploymentHandler, ProjectAccessDeploymentHandler>();
        serviceCollection.AddSingleton<ProjectAccessDeploymentHandler>();

        serviceCollection.AddTransient<IFetchService, ProjectAccessFetchService>();
        serviceCollection.AddTransient<IProjectAccessFetchHandler, ProjectAccessFetchHandler>();
        serviceCollection.AddTransient<ProjectAccessFetchHandler>();

    }
}
