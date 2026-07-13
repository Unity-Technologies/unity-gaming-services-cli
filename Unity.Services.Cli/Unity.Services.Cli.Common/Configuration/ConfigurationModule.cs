using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Common.Handlers;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.Common;

public class ConfigurationModule : ICommandModule
{
    public Command ModuleRootCommand { get; }
    internal Command GetCommand { get; }
    internal Command SetCommand { get; }
    internal Command DeleteCommand { get; }

    public ConfigurationModule()
    {
        GetCommand = new(
            "get",
            new CommandDescription("Get the value of a configuration for the given key.")
                .WithReturn("The config value string.")
                .Build())
        {
            ConfigurationInput.KeyArgument
        };
        GetCommand.SetHandler<ConfigurationInput, IConfigurationService, ISystemEnvironmentProvider,
            ILogger, CancellationToken>(GetHandler.GetAsync);

        SetCommand = new(
            "set", new CommandDescription("Update configuration with a value for the given key.")
                .WithReturn("Confirmation message.")
                .Build())
        {
            ConfigurationInput.KeyArgument,
            ConfigurationInput.ValueArgument
        };
        SetCommand.SetHandler<ConfigurationInput, IConfigurationService, ILogger, CancellationToken>(
            SetHandler.SetAsync);

        DeleteCommand = new("delete", new CommandDescription("Delete the value of a key in configuration.")
            .WithReturn("Confirmation message.")
            .Build())
        {
            ConfigurationInput.KeysOption,
            ConfigurationInput.TargetAllKeysOption,
            CommonInput.UseForceOption
        };
        DeleteCommand.SetHandler<ConfigurationInput, IConfigurationService, ILogger, ISystemEnvironmentProvider, CancellationToken>(
            DeleteTriggersHandler.DeleteAsync);

        ModuleRootCommand = new(
            "config",
            "Update configuration with a value for the given key.")
        {
            DeleteCommand,
            GetCommand,
            SetCommand
        };
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var configPath = Path.Combine(ConfigDirectory.GetPath(), "Config.json");
        var persister = new JsonFilePersister<Models.Configuration>(configPath);
        serviceCollection.AddSingleton<IPersister<Models.Configuration>>(persister);
        var configurationValidator = new ConfigurationValidator();
        var configurationService = new ConfigurationService(persister, configurationValidator);
        serviceCollection.AddSingleton<IConfigurationValidator>(configurationValidator);
        serviceCollection.AddSingleton<IConfigurationService>(configurationService);
    }
}
