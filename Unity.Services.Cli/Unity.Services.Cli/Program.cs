using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using Unity.Services.Cli.CloudCode;
using Unity.Services.Cli.Lobby;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Features;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Middleware;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Environment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.RemoteConfig;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.Deploy;

namespace Unity.Services.Cli;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        var features = await FeaturesFactory.BuildAsync(Host.CreateDefaultBuilder());
        var logger = new Logger();
        var services = new ServiceTypesBridge();
        var ansiConsole = AnsiConsole.Create(new AnsiConsoleSettings());
        TelemetrySender telemetrySender = null;
        SystemEnvironmentProvider systemEnvironmentProvider = null;

        var parser = BuildCommandLine()
            .UseHost(_ => Host.CreateDefaultBuilder(),
                host =>
                {
                    systemEnvironmentProvider = new SystemEnvironmentProvider();
                    host.UseServiceProviderFactory(_ => services);
                    CommonModule.ConfigureCommonServices(host, logger, features, ansiConsole);
                    telemetrySender = CommonModule.CreateTelemetrySender(systemEnvironmentProvider);

                    host.ConfigureServices(ConfigurationModule.RegisterServices);
                    host.ConfigureServices(AuthenticationModule.RegisterServices);
                    host.ConfigureServices(EnvironmentModule.RegisterServices);
                    host.ConfigureServices(DeployModule.RegisterServices);
                    host.ConfigureServices(CloudCodeModule.RegisterServices);
                    host.ConfigureServices(RemoteConfigModule.RegisterServices);
                    host.ConfigureServices(LobbyModule.RegisterServices);
                    host.ConfigureServices(serviceCollection => serviceCollection
                        .AddSingleton<ISystemEnvironmentProvider>(systemEnvironmentProvider));
                })
            .UseVersionOption()
            .UseHelp(ctx =>
            {
                ctx.HelpBuilder.CustomizeLayout(_ =>
                {
                    List<HelpSectionDelegate> helpSectionDelegates = HelpBuilder.Default.GetLayout().ToList();

                    helpSectionDelegates.Insert(1, _ => ansiConsole
                        .Markup($"Project Role Requirements:{System.Environment.NewLine}  You may need " +
                                $"permissions to use this module or command.{System.Environment.NewLine}  " +
                                "Visit https://github.com/Unity-Technologies/unity-gaming-services-" +
                                "cli/blob/main/docs/project-roles.md for required project roles." +
                                $"{System.Environment.NewLine}"));

                    return helpSectionDelegates.AsEnumerable();
                });
            })
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .UseExceptionHandler(
                (exception, context) =>
                {
                    var diagnostics = new Diagnostics(telemetrySender, systemEnvironmentProvider);
                    var exceptionHelper = new ExceptionHelper(diagnostics, ansiConsole);
                    exceptionHelper.HandleException(exception, logger, context);
                }
            )
            .UseTreePrinter()
            .CancelOnProcessTermination()
            .AddLoggerMiddleware(logger)
            .AddGlobalCommonOptions()
            .AddCommandInputParserMiddleware()
            .AddCliServicesMiddleware(services)
            // Manually keep modules in alphabetical order
            .AddModule(new AuthenticationModule())
            .AddModule(new CloudCodeModule())
            .AddModule(new ConfigurationModule())
            .AddModule(new DeployModule())
            .AddModule(new EnvironmentModule())
            .AddModule(new LobbyModule())
            .AddModule(new RemoteConfigModule())
            .Build();

        return await parser.InvokeAsync(args)
            .ContinueWith(commandTask =>
            {
                logger.Write();
                return commandTask.Result;
            });
    }

    static CommandLineBuilder BuildCommandLine()
    {
        var root = new RootCommand("Unity Gaming Services CLI. Use the CLI to interact with Unity Dashboard.");
        return new CommandLineBuilder(root);
    }
}
