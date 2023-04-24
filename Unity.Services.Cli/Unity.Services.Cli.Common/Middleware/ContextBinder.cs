using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.Telemetry;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Common.Middleware;

public static class ContextBinder
{
    const BindingFlags k_InputMemberFlags = BindingFlags.Instance | BindingFlags.Public;

    public static CommandLineBuilder AddCliServicesMiddleware(this CommandLineBuilder builder, IServiceTypeList serviceTypes)
    {
        builder.AddMiddleware(AddCliServicesToContext);
        return builder;

        void AddCliServicesToContext(InvocationContext context)
        {
            foreach (var serviceType in serviceTypes.ServiceTypes)
            {
                if (context.BindingContext.GetService(serviceType) != null)
                {
                    continue;
                }

                context.BindingContext.AddService(serviceType, _ =>
                {
                    var host = context.GetHost();
                    var service = host.Services.GetRequiredService(serviceType);
                    return service;
                });
            }
        }
    }

    public static CommandLineBuilder AddLoggerMiddleware(this CommandLineBuilder builder, Logger logger)
    {
        builder.AddMiddleware(AddLoggerToContext);
        void AddLoggerToContext(InvocationContext context)
        {
            context.BindingContext.AddService(typeof(ILogger), _ => logger);
        }
        return builder;
    }

    public static CommandLineBuilder AddCommandInputParserMiddleware(this CommandLineBuilder builder)
    {
        builder.AddMiddleware(AddInputParserToContext);
        return builder;

        void AddInputParserToContext(InvocationContext context)
        {
            var customInputTypes = GetCustomInputTypes();
            foreach (var inputType in customInputTypes)
            {
                context.BindingContext.AddService(inputType, _ =>
                {
                    var host = context.GetHost();
                    var configService = host.Services.GetRequiredService<IConfigurationService>();
                    var envUtilities = host.Services.GetRequiredService<ISystemEnvironmentProvider>();
                    var logger = host.Services.GetRequiredService<ILoggerProvider>().CreateLogger("");
                    var inputInstance = Activator.CreateInstance(inputType)!;
                    var memberInfos = inputType.GetMembers(k_InputMemberFlags);
                    SetInputFromEnvironment(inputInstance, envUtilities, logger, context, memberInfos);
                    SetInputFromConfigAsync(inputInstance, configService, logger, context, memberInfos).Wait();
                    SetInputFromParseResult(inputType, context.ParseResult, inputInstance);
                    SetUnityEnvironment(inputInstance, context);
                    SetAnalyticEventFactory(inputInstance, context);
                    return inputInstance;
                });
            }
        }

        static IEnumerable<Type> GetCustomInputTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.FullName?.StartsWith("Unity.Services.Cli") ?? true)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsAssignableTo(typeof(CommonInput)))
                .ToArray();
        }
    }

    static void SetAnalyticEventFactory(
        object inputInstance,
        InvocationContext context)
    {
        var host = context.GetHost();
        var eventFactory = host.Services.GetRequiredService<IAnalyticEventFactory>();
        eventFactory.ProjectId = ((CommonInput)inputInstance).CloudProjectId ?? "";
    }

    static void SetUnityEnvironment(object inputInstance, InvocationContext context)
    {
        var host = context.GetHost();
        var unityEnv = host.Services.GetRequiredService<IUnityEnvironment>();
        unityEnv.SetName(((CommonInput)inputInstance).TargetEnvironmentName);
        unityEnv.SetProjectId(((CommonInput)inputInstance).CloudProjectId);
    }

    internal static void SetInputFromParseResult(Type inputType, ParseResult parseResult, object inputInstance)
    {
        var members = inputType.GetMembers(k_InputMemberFlags)
            .Where(IsMemberBoundToInputDefinition);
        foreach (var member in members)
        {
            SetInputMember(inputType, parseResult, inputInstance, member);
        }
    }

    internal static void SetInputMember(
        IReflect inputType, ParseResult parseResult, object inputInstance, MemberInfo member)
    {
        var memberName = member.GetCustomAttribute<InputBindingAttribute>()!.InputName;
        if (!TryGetSymbol<Symbol>(inputType, memberName, out var symbol))
            return;

        var value = GetValueForSymbol(parseResult, symbol!);
        if (value is not null)
        {
            member.SetMemberValue(inputInstance, value);
        }
    }

    internal static async Task SetInputFromConfigAsync(object inputInstance, IConfigurationService config,
        ILogger logger, InvocationContext context, IEnumerable<MemberInfo> memberInfos)
    {
        var configBindingMembers = memberInfos
            .Where(IsMemberBoundToTypeDefinition<ConfigBindingAttribute>);
        foreach (var member in configBindingMembers)
        {
            await SetConfigMemberAsync(inputInstance, config, logger, context, member);
        }
    }

    internal static void SetInputFromEnvironment(object inputInstance, ISystemEnvironmentProvider envUtilities,
        ILogger logger, InvocationContext context, IEnumerable<MemberInfo> memberInfos)
    {
        var envBindingMembers = memberInfos
            .Where(IsMemberBoundToTypeDefinition<EnvironmentBindingAttribute>);
        foreach (var member in envBindingMembers)
        {
            SetEnvironmentMember(inputInstance, envUtilities, logger, context, member);
        }
    }

    internal static bool IsMemberBoundToTypeDefinition<T>(MemberInfo member)
        where T : Attribute
    {
        return member is FieldInfo or PropertyInfo
               && member.GetCustomAttribute<T>() is not null;
    }

    internal static void SetEnvironmentMember(object inputInstance, ISystemEnvironmentProvider envUtilities,
        ILogger logger, InvocationContext context, MemberInfo member)
    {
        try
        {
            var memberName = member.GetCustomAttribute<EnvironmentBindingAttribute>()!.EnvironmentKey;
            string? value = envUtilities.GetSystemEnvironmentVariable(memberName, out _);
            if (!string.IsNullOrEmpty(value))
            {
                member.SetMemberValue(inputInstance, value);
            }
        }
        catch (Exception e)
        {
            var eventFactory = new AnalyticEventFactory(new SystemEnvironmentProvider());
            var exceptionHelper = new ExceptionHelper(eventFactory.CreateDiagnosticEvent(), AnsiConsole.Create(new AnsiConsoleSettings()));
            exceptionHelper.HandleException(e, logger, context);
        }
    }

    internal static async Task SetConfigMemberAsync(object inputInstance, IConfigurationService config,
        ILogger logger, InvocationContext context, MemberInfo member)
    {
        try
        {
            var memberName = member.GetCustomAttribute<ConfigBindingAttribute>()!.ConfigName;
            string? value = await config.GetConfigArgumentsAsync(memberName);
            if (!string.IsNullOrEmpty(value))
            {
                member.SetMemberValue(inputInstance, value);
            }
        }
        catch (MissingConfigurationException)
        {
            // Do nothing, let devs handle an empty value
        }
        catch (Exception e)
        {
            var eventFactory = new AnalyticEventFactory(new SystemEnvironmentProvider());
            var exceptionHelper = new ExceptionHelper(eventFactory.CreateDiagnosticEvent(), AnsiConsole.Create(new AnsiConsoleSettings()));
            exceptionHelper.HandleException(e, logger, context);
        }
    }

    internal static object? GetValueForSymbol<TSymbol>(ParseResult parseResult, TSymbol symbol)
        where TSymbol : Symbol
        => symbol switch
        {
            Argument arg => parseResult.GetValueForArgument(arg),
            Option option => parseResult.GetValueForOption(option),
            _ => throw new ArgumentOutOfRangeException(nameof(symbol))
        };

    internal static bool TryGetSymbol<TSymbol>(IReflect inputType, string memberName, out TSymbol? symbol)
        where TSymbol : Symbol
    {
        const BindingFlags symbolFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        var member = inputType.GetMember(memberName, symbolFlags)
            .FirstOrDefault(x => x is PropertyInfo or FieldInfo);
        if (member != null
            && member.GetMemberType().IsAssignableTo(typeof(TSymbol)))
        {
            symbol = member.GetMemberValue(null) as TSymbol;
            return true;
        }

        symbol = default;
        return false;
    }

    internal static bool IsMemberBoundToInputDefinition(MemberInfo member)
    {
        return member is FieldInfo or PropertyInfo
            && member.GetCustomAttribute<InputBindingAttribute>() is not null;
    }
}
