using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Lobby.Handlers;
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
        var bulkUpdateLobbyCommand = new LobbyCommand("bulk-update", "Bulk update a lobby.")
        {
            CommonLobbyInput.LobbyIdArgument,
            RequiredBodyInput.RequestBodyArgument,
        };
        bulkUpdateLobbyCommand.SetHandler<RequiredBodyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(BulkUpdateLobbyHandler.BulkUpdateLobbyAsync);

        /* Create Lobby */
        var createLobbyCommand = new LobbyCommand("create", "Create a new lobby.")
        {
            RequiredBodyInput.RequestBodyArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        createLobbyCommand.SetHandler<RequiredBodyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(CreateLobbyHandler.CreateLobbyAsync);

        /* Delete Lobby */
        var deleteLobbyCommand = new LobbyCommand("delete", "Delete a lobby.")
        {
            CommonLobbyInput.LobbyIdArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        deleteLobbyCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(DeleteLobbyHandler.DeleteLobbyAsync);

        /* Get Joined Lobbies */
        var getJoinedLobbiesCommand = new LobbyCommand("get-joined", "Get the lobbies you are currently in.")
        {
            PlayerInput.PlayerIdArgument,
        };
        getJoinedLobbiesCommand.SetHandler<PlayerInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(GetJoinedLobbiesHandler.GetJoinedLobbiesAsync);

        /* Get Hosted Lobbies */
        var getHostedLobbiesCommand = new LobbyCommand("get-hosted", "Get the lobbies you are currently hosting.")
        {
            CommonLobbyInput.PlayerIdOption,
        };
        getHostedLobbiesCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(GetHostedLobbiesHandler.GetHostedLobbiesAsync);

        /* Get Lobby */
        var getLobbyCommand = new LobbyCommand("get", "Get a lobby.")
        {
            CommonLobbyInput.LobbyIdArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        getLobbyCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(GetLobbyHandler.GetLobbyAsync);

        /* Join Lobby */
        var joinLobbyCommand = new LobbyCommand("join", "Join a lobby by ID or code.")
        {
            JoinInput.LobbyIdOption,
            JoinInput.LobbyCodeOption,
            CommonLobbyInput.PlayerDetailsArgument,
        };
        joinLobbyCommand.SetHandler<JoinInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(JoinLobbyHandler.JoinLobbyAsync);

        /* Reconnect */
        var reconnectCommand = new LobbyCommand("reconnect", "Reconnect to a lobby.")
        {
            CommonLobbyInput.LobbyIdArgument,
            PlayerInput.PlayerIdArgument,
        };
        reconnectCommand.SetHandler<PlayerInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(ReconnectHandler.ReconnectAsync);

        /* Query Lobbies */
        var queryLobbiesCommand = new LobbyCommand("query", "Query lobbies.")
        {
            CommonLobbyInput.PlayerIdOption,
            LobbyBodyInput.JsonFileOrBodyOption,
        };
        queryLobbiesCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(QueryLobbiesHandler.QueryLobbiesAsync);

        /* Quick Join */
        var quickJoinCommand = new LobbyCommand("quickjoin", "QuickJoin a lobby.")
        {
            LobbyBodyInput.QueryFilterArgument,
            LobbyBodyInput.PlayerDetailsArgument,
        };
        quickJoinCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(QuickJoinHandler.QuickJoinAsync);

        /* Remove Player */
        var removePlayerCommand = new LobbyCommand("remove", "Remove a player from a lobby.")
        {
            CommonLobbyInput.LobbyIdArgument,
            PlayerInput.PlayerIdArgument,
        };
        removePlayerCommand.SetHandler<PlayerInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(RemovePlayerHandler.RemovePlayerAsync);

        /* Update Player */
        var updatePlayerCommand = new LobbyCommand("update", "Update a player in a lobby.")
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
        var updateLobbyCommand = new LobbyCommand("update", "Update a lobby.")
        {
            CommonLobbyInput.LobbyIdArgument,
            RequiredBodyInput.RequestBodyArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        updateLobbyCommand.SetHandler<RequiredBodyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(UpdateLobbyHandler.UpdateLobbyAsync);

        /* Heartbeat Command */
        var heartbeatLobbyCommand = new LobbyCommand("heartbeat", "Heartbeat a lobby.")
        {
            CommonLobbyInput.LobbyIdArgument,
            CommonLobbyInput.PlayerIdOption,
        };
        heartbeatLobbyCommand.SetHandler<CommonLobbyInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(HeartbeatHandler.HeartbeatLobbyAsync);

        /* Request Token Command */
        var requestTokenCommand = new LobbyCommand("request-token", "Request a token.")
        {
            CommonLobbyInput.LobbyIdArgument,
            PlayerInput.PlayerIdArgument,
            LobbyTokenInput.TokenTypeArgument,
        };
        requestTokenCommand.SetHandler<LobbyTokenInput, IUnityEnvironment, ILobbyService, ILogger, CancellationToken>(RequestTokenHandler.RequestTokenAsync);

        var configGetCommand = new Command("get", "Get a lobby config.")
        {
            CommonInput.CloudProjectIdOption,
            CommonInput.EnvironmentNameOption
        };
        configGetCommand.SetHandler<CommonInput, IUnityEnvironment, IRemoteConfigService, ILogger, CancellationToken>(ConfigGetHandler.ConfigGetAsync);

        var configUpdateCommand = new Command("update", "Update an existing lobby config.")
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

        /* Root Command */
        ModuleRootCommand = new("lobby", "Interact with the Lobby service.")
        {
            bulkUpdateLobbyCommand,
            configCommand,
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
        };
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var authenticationService = serviceProvider.GetRequiredService<IServiceAccountAuthenticationService>();
        var validator = new ConfigurationValidator();
        serviceCollection.AddSingleton<ILobbyService>(new LobbyService(validator, authenticationService, null, null));
    }
}
