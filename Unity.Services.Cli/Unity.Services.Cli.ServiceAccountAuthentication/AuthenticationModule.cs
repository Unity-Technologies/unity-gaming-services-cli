using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication.Handlers;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub;
#if FEATURE_HUB_AUTH
using Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;
using Unity.Services.Cli.ServiceAccountAuthentication.Hub.TokenExchange;
#endif
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
            new CommandDescription(
                "Authenticate the CLI. "
                + "When Unity Hub is running, logs in automatically using your Hub credentials. "
                + "Otherwise, prompts for a Service Account Key ID and Secret Key. "
                + $"You can also pass credentials via the {LoginInput.ServiceKeyIdAlias} and {LoginInput.ServiceSecretKeyAlias} options, "
                + $"or set the {AuthenticatorV1.ServiceKeyId} and {AuthenticatorV1.ServiceSecretKey} environment variables."
                + $"{Environment.NewLine}Visit {k_ServiceAccountDocLink} to create or manage a service account.")
                .WithReturn("Confirmation message indicating the authentication method used.")
                .Build())
        {
            LoginInput.ServiceKeyIdOption,
            LoginInput.SecretKeyOption,
#if FEATURE_HUB_AUTH
            LoginInput.UnityHubOption,
#endif
        };
        LoginCommand.SetHandler<LoginInput, IAuthenticator, ILogger, CancellationToken>(LoginHandler.LoginAsync);

        LogoutCommand = new(
            "logout",
            new CommandDescription("Clear stored login credentials from local configuration.")
                .WithReturn("Confirmation message.")
                .Build());
        LogoutCommand.SetHandler<IAuthenticator, ISystemEnvironmentProvider, ILogger, CancellationToken>(
            LogoutHandler.LogoutAsync);

        StatusCommand = new(
            "status",
            new CommandDescription(
                "Report which credential source the CLI will use for authenticated calls, "
                + "and warn when more than one is configured.")
                .WithReturn("Status message indicating the active credential source.")
                .Build());
        StatusCommand.SetHandler<IAuthenticator, ISystemEnvironmentProvider, IHubAuthProvider, ILogger, CancellationToken>(
            StatusHandler.GetStatusAsync);
    }

    public static void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
    {
        var credentialsPath = Path.Combine(
            ConfigDirectory.GetPath(),
            "credentials");
        var persister = new JsonFilePersister<string>(credentialsPath);
        var environmentProvider = new SystemEnvironmentProvider();

        // ReSharper disable once RedundantAssignment
        IHubAuthProvider hubAuthProvider = new NullHubAuthProvider();
#if FEATURE_HUB_AUTH
        var hubTransport = new HubIpcTransport();
        var hubClient = new HubIpcClient(hubTransport);
        var tokenExchangeClient = new TokenExchangeClient(new HttpClient());
        hubAuthProvider = new HubAuthProvider(hubClient, tokenExchangeClient);
#endif

        serviceCollection.AddSingleton(hubAuthProvider);
        serviceCollection.AddSingleton<IAuthenticator>(s =>
            new AuthenticatorV1(persister, s.GetRequiredService<IConsolePrompt>(), s.GetRequiredService<IHubAuthProvider>()));
        serviceCollection.AddSingleton<IServiceAccountAuthenticationService>(s =>
            new AuthenticationService(persister, environmentProvider, s.GetRequiredService<IHubAuthProvider>()));
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
