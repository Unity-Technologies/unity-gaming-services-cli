using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Player.Handlers;
using Unity.Services.Cli.Player.Input;
using Unity.Services.Cli.Player.Networking;
using Unity.Services.Cli.Player.Service;
using Unity.Services.Gateway.PlayerAdminApiV2.Generated.Api;
using Unity.Services.Gateway.PlayerAuthApiV1.Generated.Api;

namespace Unity.Services.Cli.Player;

/// <summary>
/// A Template module to achieve a get request command: ugs player get `address` -o `file`
/// </summary>
public class PlayerModule : ICommandModule
{
    public Command? ModuleRootCommand { get; }
    internal Command? DeleteCommand { get; }
    internal Command? CreateCommand { get; }

    internal Command? EnableCommand { get; }

    internal Command? DisableCommand { get; }

    public PlayerModule()
    {
        CreateCommand = new Command("create", "Create player account in Unity Authentication Service")
        {
            CommonInput.CloudProjectIdOption
        };
        CreateCommand.SetHandler<PlayerInput, IPlayerService, ILogger, ILoadingIndicator, CancellationToken>(CreateHandler.CreateAsync);

        DeleteCommand = new Command("delete", "Delete player account in Unity Authentication Service")
        {
            CommonInput.CloudProjectIdOption,
            PlayerInput.PlayerIdArgument
        };
        DeleteCommand.SetHandler<PlayerInput, IPlayerService, ILogger, ILoadingIndicator, CancellationToken>(DeleteHandler.DeleteAsync);

        DisableCommand = new Command("disable", "Disable player account in Unity Authentication Service")
        {
            CommonInput.CloudProjectIdOption,
            PlayerInput.PlayerIdArgument
        };
        DisableCommand.SetHandler<PlayerInput, IPlayerService, ILogger, ILoadingIndicator, CancellationToken>(DisableHandler.DisableAsync);

        EnableCommand = new Command("enable", "Enable player account in Unity Authentication Service")
        {
            CommonInput.CloudProjectIdOption,
            PlayerInput.PlayerIdArgument
        };
        EnableCommand.SetHandler<PlayerInput, IPlayerService, ILogger, ILoadingIndicator, CancellationToken>(EnableHandler.EnableAsync);

        ModuleRootCommand = new("player", "Manage your player accounts in Unity Authentication Service")
        {
            CreateCommand,
            DeleteCommand,
            DisableCommand,
            EnableCommand
        };
    }

    /// <summary>
    /// Register service to UGS CLI host builder
    /// </summary>
    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {

        var playerAdminConfig = new Gateway.PlayerAdminApiV2.Generated.Client.Configuration()
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<PlayerAdminEndpoints>()
        };

        var playerAuthConfig = new Gateway.PlayerAuthApiV1.Generated.Client.Configuration()
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<PlayerAuthEndpoints>()
        };

        playerAdminConfig.DefaultHeaders.SetXClientIdHeader();
        playerAuthConfig.DefaultHeaders.SetXClientIdHeader();

        serviceCollection.AddSingleton<IPlayerAuthenticationAdminApiAsync>(new PlayerAuthenticationAdminApi(playerAdminConfig));
        serviceCollection.AddSingleton<IDefaultApiAsync>(new DefaultApi(playerAuthConfig));

        serviceCollection.AddTransient<IConfigurationValidator, ConfigurationValidator>();
        serviceCollection.AddTransient<IPlayerService, PlayerService>();
    }
}
