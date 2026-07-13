using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using Unity.Services.Cli.Authoring.Compression;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.ImportExport;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;

namespace Unity.Services.Cli.Lobby;

/// <summary>
/// Module implementing the CLI for the Lobby service.
/// </summary>
public class LobbyModule : ICommandModule
{
    public Command ModuleRootCommand { get; }

    /// <summary>
    /// The base class defining the shared Lobby command arguments and options.
    /// </summary>
    public class LobbyCommand : Command
    {
        public LobbyCommand(string name, string? description = null) : base(name, description)
        {
            Add(CommonLobbyInput.ServiceIdOption);
            AddOption(CommonInput.CloudProjectIdOption);
            AddOption(CommonInput.EnvironmentNameOption);
        }
    }

    public LobbyModule()
    {
        /* Bulk Update Lobby */
        var bulkUpdateLobbyCommand = new LobbyCommand("bulk-update", new CommandDescription("Bulk update a lobby.")
            .WithReturn("The full updated lobby JSON object.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            RequiredBodyInput.RequestBodyArgument,
        };
        bulkUpdateLobbyCommand.SetHandler<RequiredBodyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(BulkUpdateLobbyHandler.BulkUpdateLobbyAsync);

        /* Create Lobby */
        var createLobbyCommand = new LobbyCommand("create", new CommandDescription("Create a new lobby.")
            .WithReturn("The full created lobby JSON object.")
            .Build())
        {
            RequiredBodyInput.RequestBodyArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        createLobbyCommand.SetHandler<RequiredBodyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(CreateLobbyHandler.CreateLobbyAsync);

        /* Delete Lobby */
        var deleteLobbyCommand = new LobbyCommand("delete", new CommandDescription("Delete a lobby.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        deleteLobbyCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(DeleteLobbyHandler.DeleteLobbyAsync);

        /* Get Joined Lobbies */
        var getJoinedLobbiesCommand = new LobbyCommand("get-joined", new CommandDescription("Get the lobbies you are currently in.")
            .WithReturn("JSON array of lobby ID strings.")
            .Build())
        {
            PlayerInput.PlayerIdArgument,
        };
        getJoinedLobbiesCommand.SetHandler<PlayerInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(GetJoinedLobbiesHandler.GetJoinedLobbiesAsync);

        /* Get Hosted Lobbies */
        var getHostedLobbiesCommand = new LobbyCommand("get-hosted", new CommandDescription("Get the lobbies you are currently hosting.")
            .WithReturn("JSON array of lobby ID strings.")
            .Build())
        {
            CommonLobbyInput.PlayerIdOption,
        };
        getHostedLobbiesCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(GetHostedLobbiesHandler.GetHostedLobbiesAsync);

        /* Get Lobby */
        var getLobbyCommand = new LobbyCommand("get", new CommandDescription("Get a lobby.")
            .WithReturn("The full lobby JSON object.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        getLobbyCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(GetLobbyHandler.GetLobbyAsync);

        /* Join Lobby */
        var joinLobbyCommand = new LobbyCommand("join", new CommandDescription("Join a lobby by ID or code.")
            .WithReturn("The full lobby JSON object.")
            .Build())
        {
            JoinInput.LobbyIdOption,
            JoinInput.LobbyCodeOption,
            CommonLobbyInput.PlayerDetailsArgument,
        };
        joinLobbyCommand.SetHandler<JoinInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(JoinLobbyHandler.JoinLobbyAsync);

        /* Reconnect */
        var reconnectCommand = new LobbyCommand("reconnect", new CommandDescription("Reconnect to a lobby.")
            .WithReturn("The full lobby JSON object.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            PlayerInput.PlayerIdArgument,
        };
        reconnectCommand.SetHandler<PlayerInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(ReconnectHandler.ReconnectAsync);

        /* Query Lobbies */
        var queryLobbiesCommand = new LobbyCommand("query", new CommandDescription("Query lobbies.")
            .WithReturn("JSON query response with a paginated list of matching lobbies.")
            .Build())
        {
            CommonLobbyInput.PlayerIdOption,
            LobbyBodyInput.JsonFileOrBodyOption,
        };
        queryLobbiesCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(QueryLobbiesHandler.QueryLobbiesAsync);

        /* Quick Join */
        var quickJoinCommand = new LobbyCommand("quickjoin", new CommandDescription("QuickJoin a lobby.")
            .WithReturn("The full lobby JSON object.")
            .Build())
        {
            LobbyBodyInput.QueryFilterArgument,
            LobbyBodyInput.PlayerDetailsArgument,
        };
        quickJoinCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(QuickJoinHandler.QuickJoinAsync);

        /* Remove Player */
        var removePlayerCommand = new LobbyCommand("remove", new CommandDescription("Remove a player from a lobby.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            PlayerInput.PlayerIdArgument,
        };
        removePlayerCommand.SetHandler<PlayerInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(RemovePlayerHandler.RemovePlayerAsync);

        /* Update Player */
        var updatePlayerCommand = new LobbyCommand("update", new CommandDescription("Update a player in a lobby.")
            .WithReturn("The full updated lobby JSON object.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            PlayerInput.PlayerIdArgument,
            RequiredBodyInput.RequestBodyArgument,
        };
        updatePlayerCommand.SetHandler<PlayerInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(UpdatePlayerHandler.UpdatePlayerAsync);

        /* Base Player Command */
        var playerCommand = new Command("player", "Update or remove a player in a lobby.")
        {
            updatePlayerCommand,
            removePlayerCommand,
        };

        /* Update Lobby Command */
        var updateLobbyCommand = new LobbyCommand("update", new CommandDescription("Update a lobby.")
            .WithReturn("The full updated lobby JSON object.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            RequiredBodyInput.RequestBodyArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        updateLobbyCommand.SetHandler<RequiredBodyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(UpdateLobbyHandler.UpdateLobbyAsync);

        /* Heartbeat Command */
        var heartbeatLobbyCommand = new LobbyCommand("heartbeat", new CommandDescription("Heartbeat a lobby.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        heartbeatLobbyCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(HeartbeatHandler.HeartbeatLobbyAsync);

        /* Request Token Command */
        var requestTokenCommand = new LobbyCommand("request-token", new CommandDescription("Request a token.")
            .WithReturn("JSON dictionary mapping token type names to token data objects.")
            .Build())
        {
            CommonLobbyInput.LobbyIdArgument,
            PlayerInput.PlayerIdArgument,
            LobbyTokenInput.TokenTypeArgument,
        };
        requestTokenCommand.SetHandler<LobbyTokenInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(RequestTokenHandler.RequestTokenAsync);

        var configGetCommand = new Command("get", new CommandDescription("Get a lobby config.")
            .WithReturn("Raw JSON string of the lobby remote config.")
            .Build())
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption
        };
        configGetCommand.SetHandler<CommonInput, IUnityEnvironment, IRemoteConfigService, ILogger, CancellationToken>(ConfigGetHandler.ConfigGetAsync);

        var configUpdateCommand = new Command("update", new CommandDescription("Update an existing lobby config.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            LobbyConfigUpdateInput.ConfigIdArgument,
            RequiredBodyInput.RequestBodyArgument,
            CommonInput.CloudProjectIdOption
        };
        configUpdateCommand.SetHandler<LobbyConfigUpdateInput, IRemoteConfigService, ILogger, CancellationToken>(ConfigUpdateHandler.ConfigUpdateAsync);

        var configCommand = new Command("config", "Get or update a lobby config.")
        {
            configGetCommand,
            configUpdateCommand,
        };

        var importCommand = new Command("import", new CommandDescription("Import lobby configuration into an environment.")
            .WithReturn("Summary of created, updated, and deleted items.")
            .Build())
        {
            ImportInput.InputDirectoryArgument,
            ImportInput.FileNameArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ImportInput.DryRunOption,
            ImportInput.ReconcileOption,
        };
        importCommand.SetHandler<
            ImportInput,
            LobbyImporter,
            ILoadingIndicator,
            CancellationToken>(
            ImportHandler.ImportAsync);

        var exportCommand = new Command("export", new CommandDescription("Export lobby configuration from an environment.")
            .WithReturn("Summary of exported items.")
            .Build())
        {
            ExportInput.OutputDirectoryArgument,
            ExportInput.FileNameArgument,
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption,
            ExportInput.DryRunOption,
        };
        exportCommand.SetHandler<
            ExportInput,
            LobbyExporter,
            ILoadingIndicator,
            CancellationToken>(ExportHandler.ExportAsync);



        /* Root Command */
        ModuleRootCommand = new("lobby", new CommandDescription("Interact with the Lobby service.")
            .WithDocs("https://docs.unity.com/ugs/manual/lobby/manual")
            .WithClientApi("https://services.docs.unity.com/lobby/v1/")
            .Build())
        {
            bulkUpdateLobbyCommand,
            createLobbyCommand,
            deleteLobbyCommand,
            getHostedLobbiesCommand,
            getJoinedLobbiesCommand,
            getLobbyCommand,
            heartbeatLobbyCommand,
            joinLobbyCommand,
            playerCommand,
            queryLobbiesCommand,
            quickJoinCommand,
            reconnectCommand,
            requestTokenCommand,
            updateLobbyCommand,
            configCommand,
            importCommand,
            exportCommand,
        };
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var validator = new ConfigurationValidator();
        serviceCollection.AddSingleton<ILobbyService>(s =>
            new LobbyService(validator, s.GetRequiredService<IServiceAccountAuthenticationService>(), null, null));
        serviceCollection.AddTransient<LobbyImporter, LobbyImporter>();
        serviceCollection.AddTransient<LobbyExporter, LobbyExporter>();
    }
}
