using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Policies;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Environment.Handlers;
using Unity.Services.Cli.Environment.Input;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.IdentityApiV1.Generated.Api;

namespace Unity.Services.Cli.Environment;

public class EnvironmentModule : ICommandModule
{
    public Command ModuleRootCommand { get; }

    internal Command ListCommand { get; }

    internal Command AddCommand { get; }

    internal Command DeleteCommand { get; }

    internal Command UseCommand { get; }

    public EnvironmentModule()
    {
        ListCommand = new(
            "list",
            "List environments")
        {
            CommonInput.CloudProjectIdOption
        };
        ListCommand.SetHandler<
            EnvironmentInput,
            IEnvironmentService,
            IConfigurationService,
            IConsoleTable,
            ILogger,
            ILoadingIndicator,
            CancellationToken>
            (ListHandler.ListAsync);

        AddCommand = new(
            "add",
            "Add a new environment")
        {
            EnvironmentInput.EnvironmentNameArgument,
            CommonInput.CloudProjectIdOption
        };
        AddCommand.SetHandler<EnvironmentInput, IEnvironmentService, ILogger, ILoadingIndicator, CancellationToken>(
            AdditionHandler.AddAsync);

        DeleteCommand = new(
            "delete",
            "Delete an environment. User needs to authenticate with admin/owner permission to execute this command.")
        {
            EnvironmentInput.EnvironmentNameArgument,
            CommonInput.CloudProjectIdOption
        };
        DeleteCommand.SetHandler<EnvironmentInput, IEnvironmentService, ILogger, ILoadingIndicator, IUnityEnvironment,
            CancellationToken>(
            DeletionHandler.DeleteAsync);

        UseCommand = new(
            "use",
            "Select the environment to use")
        {
            EnvironmentInput.EnvironmentNameArgument
        };
        UseCommand.SetHandler<EnvironmentInput, IConfigurationService, ILogger, CancellationToken>(
            UseHandler.UseAsync);

        ModuleRootCommand = new(
            "env",
            "Access or modify Unity services environments.")
        {
            AddCommand,
            DeleteCommand,
            ListCommand,
            UseCommand
        };
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var apiAsync = serviceProvider.GetRequiredService<IEnvironmentApi>();
        var validator = new ConfigurationValidator();
        var authenticationService = serviceProvider.GetRequiredService<IServiceAccountAuthenticationService>();
        var environmentService = new EnvironmentService(apiAsync, validator, authenticationService);
        serviceCollection.AddSingleton<IEnvironmentService>(environmentService);

        var unityEnvironment = new UnityEnvironment(environmentService, validator);
        serviceCollection.AddSingleton<IUnityEnvironment>(unityEnvironment);

        // Set retry policy
        Gateway.IdentityApiV1.Generated.Client.RetryConfiguration.RetryPolicy =
            RetryPolicy.GetHttpRetryPolicy();
        Gateway.IdentityApiV1.Generated.Client.RetryConfiguration.AsyncRetryPolicy =
            RetryPolicy.GetAsyncHttpRetryPolicy();
    }
}
