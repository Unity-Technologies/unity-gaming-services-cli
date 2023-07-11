using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.CloudCode;
using Unity.Services.Cli.Lobby;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Middleware;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Environment;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.RemoteConfig;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;
using Unity.Services.Cli.Authoring;
using Unity.Services.Cli.GameServerHosting;
#if FEATURE_LEADERBOARDS
using Unity.Services.Cli.Leaderboards;
#endif
using Unity.Services.Cli.Player;
using Unity.Services.Cli.Access;

namespace Unity.Services.Cli;

public static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await InternalMain(args, new Logger());
    }

    public static async Task<int> InternalMain(string[] args, Logger logger)
    {
        var services = new ServiceTypesBridge();
        var ansiConsole = AnsiConsole.Create(new AnsiConsoleSettings());
        TelemetrySender telemetrySender = null;
        SystemEnvironmentProvider systemEnvironmentProvider = new SystemEnvironmentProvider();
        IAnalyticEventFactory analyticEventFactory = new AnalyticEventFactory(systemEnvironmentProvider);

        var parser = BuildCommandLine()
            .UseHost(
                _ => Host.CreateDefaultBuilder(),
                host =>
                {
                    host.UseServiceProviderFactory(_ => services);
                    CommonModule.ConfigureCommonServices(
                        host,
                        logger,
                        ansiConsole,
                        analyticEventFactory);
                    telemetrySender = CommonModule.CreateTelemetrySender(systemEnvironmentProvider);

                    host.ConfigureServices(ConfigurationModule.RegisterServices);
                    host.ConfigureServices(AuthenticationModule.RegisterServices);
                    host.ConfigureServices(EnvironmentModule.RegisterServices);
                    host.ConfigureServices(DeployModule.RegisterServices);
                    host.ConfigureServices(CloudCodeModule.RegisterServices);
                    host.ConfigureServices(RemoteConfigModule.RegisterServices);
                    host.ConfigureServices(AccessModule.RegisterServices);
                    host.ConfigureServices(GameServerHostingModule.RegisterServices);
                    host.ConfigureServices(LobbyModule.RegisterServices);
#if FEATURE_LEADERBOARDS
                    host.ConfigureServices(LeaderboardsModule.RegisterServices);
#endif
                    host.ConfigureServices(PlayerModule.RegisterServices);
                    host.ConfigureServices(serviceCollection => serviceCollection
                        .AddSingleton<ISystemEnvironmentProvider>(systemEnvironmentProvider));
                })
            .UseVersionOption()
            .UseHelp(
                ctx =>
                {
                    ctx.HelpBuilder.CustomizeLayout(
                        _ =>
                        {
                            List<HelpSectionDelegate> helpSectionDelegates = HelpBuilder.Default.GetLayout().ToList();

                            helpSectionDelegates.Insert(
                                1,
                                _ => ansiConsole
                                    .Markup(
                                        $"Project Role Requirements:{System.Environment.NewLine}  You may need " +
                                        $"permissions to use this module or command.{System.Environment.NewLine}  " +
                                        "Visit https://services.docs.unity.com/guides/ugs-cli/latest/general/" +
                                        "troubleshooting/project-roles for required project roles." +
                                        $"{System.Environment.NewLine}"));

                            // Replace built-in subcommand help section by custom subcommand help section
                            helpSectionDelegates.Remove(HelpBuilder.Default.SubcommandsSection());
                            helpSectionDelegates.Add(SubcommandsSectionDelegate(ctx, ansiConsole));

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
                    var diagnostics = analyticEventFactory.CreateDiagnosticEvent();
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
            .AddModule(new AuthenticationModule())
            .AddModule(new CloudCodeModule())
            .AddModule(new ConfigurationModule())
            .AddModule(new DeployModule())
            .AddModule(new FetchModule())
            .AddModule(new AccessModule())
            .AddModule(new EnvironmentModule())
#if FEATURE_LEADERBOARDS
            .AddModule(new LeaderboardsModule())
#endif
            .AddModule(new LobbyModule())
            .AddModule(new GameServerHostingModule())
            .AddModule(new PlayerModule())
            .AddModule(new RemoteConfigModule())
            .Build();

        return await parser
            .InvokeAsync(args, console: new LoggerConsole(logger.StdOut, logger.StdErr))
            .ContinueWith(
                commandTask =>
                {
                    logger.Write();
                    TrySendCommandUsageMetric(analyticEventFactory, parser.Parse(args));
                    return commandTask.Result;
                });
    }

    static CommandLineBuilder BuildCommandLine()
    {
        var root = new RootCommand("Unity Gaming Services CLI. Use the CLI to interact with Unity Dashboard.");
        return new CommandLineBuilder(root);
    }

    static HelpSectionDelegate SubcommandsSectionDelegate(HelpContext context, IAnsiConsole ansiConsole)
    {
        var subcommands = context.Command.Subcommands
            .OrderBy(command => command.Name)
            .Where(command => !command.IsHidden)
            .Select(command => context.HelpBuilder.GetTwoColumnRow(command, context))
            .ToArray();

        return WriteSubcommands();

        HelpSectionDelegate WriteSubcommands() => helpContext
            =>
        {
            if (subcommands.Length <= 0)
                return;

            ansiConsole.Markup($"Commands:{System.Environment.NewLine}");
            helpContext.HelpBuilder.WriteColumns(subcommands, helpContext);
        };
    }

    static void TrySendCommandUsageMetric(IAnalyticEventFactory analyticEventFactory, ParseResult parseResult)
    {
        var options = parseResult.Tokens
            .Where(t => t.Type == TokenType.Option)
            .Select(t => t.ToString())
            .ToArray();

        var command = AnalyticEventUtils.ConvertSymbolResultToString(parseResult.CommandResult);

        var analyticEvent = analyticEventFactory.CreateMetricEvent();
        analyticEvent.AddData("command", command);
        analyticEvent.AddData("options", options);
        analyticEvent.AddData("time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        analyticEvent.Send();
    }
}
