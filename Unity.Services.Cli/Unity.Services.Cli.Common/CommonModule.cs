using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Spectre.Console;
using Unity.Analytics.Sender;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Process;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;
using Unity.Services.Gateway.IdentityApiV1.Generated.Api;
using IdentityClient = Unity.Services.Gateway.IdentityApiV1.Generated.Client;

namespace Unity.Services.Cli.Common;

public static class CommonModule
{
    public const string m_CliProductName = "com.unity.ugs-cli";
    public static CommandLineBuilder UseTreePrinter(this CommandLineBuilder builder)
    {
        var printTreeFlag = new Option<bool>("--print-tree")
        {
            IsHidden = true,
            IsRequired = false,
        };
        var printTreeShowHiddenFlag = new Option<bool>("--print-tree-show-hidden")
        {
            IsHidden = true,
            IsRequired = false,
        };
        builder.Command.AddOption(printTreeFlag);
        builder.Command.AddOption(printTreeShowHiddenFlag);
        builder.AddMiddleware((context, next) =>
        {
            var shouldPrintTree = context.ParseResult.GetValueForOption(printTreeFlag);
            if (shouldPrintTree)
            {
                var shouldShowHidden = context.ParseResult.GetValueForOption(printTreeShowHiddenFlag);
                var treePrinter = new CommandTreePrinter(context, context.Console.Out.CreateTextWriter());
                treePrinter.PrintUsage(context.ParseResult.CommandResult.Command, shouldShowHidden);
                return Task.CompletedTask;
            }

            return next(context);
        });
        return builder;
    }

    public static void ConfigureCommonServices(IHostBuilder hostBuilder, Logger logger,
        IAnsiConsole ansiConsole, IAnalyticEventFactory analyticEventFactory)
    {
        var parseResult = hostBuilder.GetInvocationContext().ParseResult;
        bool silentAnsiConsole = parseResult.GetValueForOption(CommonInput.QuietOption) ||
                       parseResult.GetValueForOption(CommonInput.JsonOutputOption);
        var allDefinedTypesInDomain = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.DefinedTypes);
        EndpointHelper.InitializeNetworkTargetEndpoints(allDefinedTypesInDomain);
        var usedConsole = silentAnsiConsole ? null : ansiConsole;
        hostBuilder.ConfigureAppConfiguration(ConfigAppConfiguration);
        hostBuilder.ConfigureLogging(logBuilder => ConfigureLogging(parseResult, logBuilder, logger));
        hostBuilder.ConfigureServices(collection => collection.AddSingleton<ILogger>(logger));
        hostBuilder.ConfigureServices(collection => collection.AddSingleton(analyticEventFactory));
        hostBuilder.ConfigureServices(CreateAndRegisterIdentityApiServices);
        hostBuilder.ConfigureServices(serviceCollection =>
            CreateAndRegisterProgressBarService(serviceCollection, usedConsole));
        hostBuilder.ConfigureServices(serviceCollection =>
            CreateAndRegisterLoadingIndicatorService(serviceCollection, usedConsole));
        hostBuilder.ConfigureServices(CreateAndRegisterCliPromptService);
        hostBuilder.ConfigureServices(CreateAndRegisterCliAnalyticsSenderService);
        hostBuilder.ConfigureServices(CreateAndRegisterCliProcessService);
    }

    internal static void ConfigAppConfiguration(IConfigurationBuilder config)
    {
        config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true,
            reloadOnChange: true);
    }

    static void ConfigureLogging(ParseResult result, ILoggingBuilder logBuilder, Logger logger)
    {
        AddLogger(logBuilder);
        var isQuiet = result.GetValueForOption(CommonInput.QuietOption);
        if (isQuiet)
        {
            logBuilder.AddFilter(level => level >= LogLevel.Error);
        }

        var isJson = result.GetValueForOption(CommonInput.JsonOutputOption);
        logBuilder.Services.Configure<LogConfiguration>(config => config.IsJson = isJson);
        logger.Configuration.IsJson = isJson;
        logger.Configuration.IsQuiet = isQuiet;
    }

    static void AddLogger(ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddConfiguration();
        builder.Services.AddSingleton<ILoggerProvider, LoggerProvider>();
        LoggerProviderOptions.RegisterProviderOptions<LogConfiguration, LoggerProvider>(builder.Services);
    }

    internal static void CreateAndRegisterIdentityApiServices(IServiceCollection serviceCollection)
    {
        var config = new IdentityClient.Configuration
        {
            BasePath = EndpointHelper.GetCurrentEndpointFor<UnityServicesGatewayEndpoints>(),
        };
        config.DefaultHeaders.SetXClientIdHeader();

        var apiAsync = new EnvironmentApi(config);
        serviceCollection.AddSingleton<IEnvironmentApi>(apiAsync);
    }

    internal static void CreateAndRegisterProgressBarService(IServiceCollection serviceCollection,
        IAnsiConsole? ansiConsole)
    {
        serviceCollection.AddSingleton<IProgressBar>(new ProgressBar(ansiConsole));
    }

    internal static void CreateAndRegisterLoadingIndicatorService(IServiceCollection serviceCollection,
        IAnsiConsole? ansiConsole)
    {
        serviceCollection.AddSingleton<ILoadingIndicator>(new LoadingIndicator(ansiConsole));
    }

    internal static void CreateAndRegisterCliPromptService(IServiceCollection serviceCollection)
    {
        var settings = new AnsiConsoleSettings
        {
            Interactive = InteractionSupport.Yes
        };
        var console = AnsiConsole.Create(settings);
        serviceCollection.AddSingleton<ICliPrompt>(new CliPrompt(console, System.Console.IsInputRedirected));
    }

    public static TelemetrySender CreateTelemetrySender(ISystemEnvironmentProvider systemEnvironmentProvider)
    {
        var telemetryBasePath = EndpointHelper.GetCurrentEndpointFor<TelemetryApiEndpoints>();
        var telemetryApi = new TelemetryApi.Generated.Api.TelemetryApi(telemetryBasePath);

        var commonTags = new Dictionary<string, string>
        {
            [TagKeys.OperatingSystem] = Environment.OSVersion.ToString(),
            [TagKeys.Platform] = TelemetryConfigurationProvider.GetOsPlatform(),
        };
        var productTags = new Dictionary<string, string>
        {
            [TagKeys.ProductName] = m_CliProductName,
            [TagKeys.CliVersion] = TelemetryConfigurationProvider.GetCliVersion()
        };

        string cicdPlatform = TelemetryConfigurationProvider.GetCicdPlatform(systemEnvironmentProvider);

        if (!string.IsNullOrEmpty(cicdPlatform))
        {
            commonTags[TagKeys.CicdPlatform] = cicdPlatform;
        }

        return new TelemetrySender(telemetryApi, commonTags, productTags);
    }

    internal static void CreateAndRegisterCliAnalyticsSenderService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddAnalytics(((x, _) =>
                x.WithSourceName(m_CliProductName)
                    .WithDefaultUnityBigQueryExporter()
                    .WithCommonHeader(new Dictionary<string, string>
                    {
                        ["uuid"] = ""
                    })
            ));
        var provider = serviceCollection.BuildServiceProvider();
        provider.InitAnalytics();
    }

    internal static void CreateAndRegisterCliProcessService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<ICliProcess, CliProcess>();
    }
}
