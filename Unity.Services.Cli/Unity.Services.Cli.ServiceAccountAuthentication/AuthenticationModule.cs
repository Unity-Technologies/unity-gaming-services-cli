using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication.Handlers;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;

namespace Unity.Services.Cli.ServiceAccountAuthentication;

public class AuthenticationModule : ICommandModule
{
    const string k_ServiceAccountDocLink = "https://services.docs.unity.com/docs/service-account-auth";

    internal Command LoginCommand { get; }
    internal Command LogoutCommand { get; }
    internal Command StatusCommand { get; }

    public AuthenticationModule()
    {
        LoginCommand = new(
            "login",
            "Save Service Account Key ID and Secret Key. "
            + "You can save the Key ID and Secret Key with this command or system environment variables: "
            + $"{AuthenticatorV1.ServiceKeyId} and {AuthenticatorV1.ServiceSecretKey}."
            + $"{Environment.NewLine}Visit {k_ServiceAccountDocLink} to create or manage service account.")
        {
            LoginInput.ServiceKeyIdOption,
            LoginInput.SecretKeyOption,
        };
        LoginCommand.SetHandler<LoginInput, IAuthenticator, ILogger, CancellationToken>(LoginHandler.LoginAsync);

        LogoutCommand = new(
            "logout",
            "Clear stored Service Account Key ID and Secret Key from local configuration.");
        LogoutCommand.SetHandler<IAuthenticator, ISystemEnvironmentProvider, ILogger, CancellationToken>(
            LogoutHandler.LogoutAsync);

        StatusCommand = new(
            "status",
            "Checks if the current user has any Service Account Key ID" +
            " and Secret Key stored locally.");
        StatusCommand.SetHandler<IAuthenticator, ISystemEnvironmentProvider, ILogger, CancellationToken>(
            StatusHandler.GetStatusAsync);
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var credentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UnityServices/credentials");
        var persister = new JsonFilePersister<string>(credentialsPath);
        var environmentProvider = new SystemEnvironmentProvider();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var cliPrompt = serviceProvider.GetRequiredService<ICliPrompt>();
        var authenticator = new AuthenticatorV1(persister, cliPrompt);
        serviceCollection.AddSingleton<IAuthenticator>(authenticator);

        var authenticationService = new AuthenticationService(persister, environmentProvider);
        serviceCollection.AddSingleton<IServiceAccountAuthenticationService>(authenticationService);
    }

    public IEnumerable<Command> GetCommandsForCliRoot()
        => new[]
        {
            LoginCommand,
            LogoutCommand,
            StatusCommand,
        };

#pragma warning disable S1168
    /// <remarks>
    /// Disable S1168 warning ("Return empty collection") as it makes sense to return a null command for us.
    /// </remarks>
    Command? ICommandModule.ModuleRootCommand => null;
#pragma warning restore S1168
}
