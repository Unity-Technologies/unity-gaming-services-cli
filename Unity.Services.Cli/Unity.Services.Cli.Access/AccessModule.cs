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
    internal Command GetPlayerPolicyCommand;
    internal Command GetProjectPolicyCommand;
    internal Command GetAllPlayerPoliciesCommand;
    internal Command UpsertProjectPolicyCommand;
    internal Command UpsertPlayerPolicyCommand;
    internal Command DeleteProjectPolicyStatementsCommand;
    internal Command DeletePlayerPolicyStatementsCommand;
    public Command ModuleRootCommand { get; }


    public AccessModule()
    {
        GetProjectPolicyCommand = new Command("get-project-policy", "retrieves policies for a project and environment")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption
        };

        GetProjectPolicyCommand
            .SetHandler<CommonInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator, CancellationToken>(
                GetProjectPolicyHandler.GetProjectPolicyAsync);

        GetPlayerPolicyCommand = new Command("get-player-policy",
            "retrieves policies for a player in a project and environment")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            AccessInput.PlayerIdArgument
        };

        GetPlayerPolicyCommand
            .SetHandler<AccessInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator,
                CancellationToken>(GetPlayerPolicyHandler.GetPlayerPolicyAsync);

        GetAllPlayerPoliciesCommand = new Command("get-all-player-policies",
            "retrieves all players policies for a project and environment")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
        };

        GetAllPlayerPoliciesCommand
            .SetHandler<CommonInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator,
                CancellationToken>(GetAllPlayerPoliciesHandler.GetAllPlayerPoliciesAsync);

        UpsertProjectPolicyCommand = new Command("upsert-project-policy",
            "upsert statement in project policy")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            AccessInput.FilePathArgument,
        };

        UpsertProjectPolicyCommand
            .SetHandler<AccessInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator,
                CancellationToken>(UpsertProjectPolicyHandler.UpsertProjectPolicyAsync);

        UpsertPlayerPolicyCommand = new Command("upsert-player-policy",
            "upsert statements in player policy")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            AccessInput.PlayerIdArgument,
            AccessInput.FilePathArgument,
        };

        UpsertPlayerPolicyCommand
            .SetHandler<AccessInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator,
                CancellationToken>(UpsertPlayerPolicyHandler.UpsertPlayerPolicyAsync);

        DeleteProjectPolicyStatementsCommand =
            new Command("delete-project-policy-statements", "delete statements in project policy")
            {
                CommonInput.CloudProjectIdOption,
                CommonInput.EnvironmentNameOption,
                AccessInput.FilePathArgument
            };

        DeleteProjectPolicyStatementsCommand
            .SetHandler<AccessInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator,
                CancellationToken>(DeleteProjectPolicyStatementsHandler.DeleteProjectPolicyStatementsAsync);

        DeletePlayerPolicyStatementsCommand =
            new Command("delete-player-policy-statements", "delete statements in player policy")
            {
                CommonInput.CloudProjectIdOption,
                CommonInput.EnvironmentNameOption,
                AccessInput.PlayerIdArgument,
                AccessInput.FilePathArgument
            };

        DeletePlayerPolicyStatementsCommand
            .SetHandler<AccessInput, IUnityEnvironment, IAccessService, ILogger, ILoadingIndicator,
                CancellationToken>(DeletePlayerPolicyStatementsHandler.DeletePlayerPolicyStatementsAsync);

        ModuleRootCommand = new Command("access", "Manage resource policies to restrict read/write access")
        {
            GetProjectPolicyCommand,
            GetPlayerPolicyCommand,
            GetAllPlayerPoliciesCommand,
            UpsertProjectPolicyCommand,
            UpsertPlayerPolicyCommand,
            DeleteProjectPolicyStatementsCommand,
            DeletePlayerPolicyStatementsCommand,
            ModuleRootCommand.AddNewFileCommand<NewProjectAccessFile>("ProjectAccess"),
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
            BasePath = EndpointHelper.GetCurrentEndpointFor<AccessEndpoints>()
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
