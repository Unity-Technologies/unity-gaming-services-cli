using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Middleware;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.Common.Services;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.Common.Telemetry.AnalyticEvent.AnalyticEventFactory;
using Unity.Services.Cli.Common.Utils;
using ContextBinder = Unity.Services.Cli.Common.Middleware.ContextBinder;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class ContextBinderTests
{
    interface IFakeService { }

    class FakeService : IFakeService { }

    const int k_TestIntValue = 1234;
    const string k_TestStringValue = "TestString";
    const string k_Command = "test";
    private const string k_GetConfigMockedReturnValue = "value-from-mocked-config";

    CommandLineBuilder? m_CommandLineBuilder;
    TestInput? m_TestInput;

    [OneTimeSetUp]
    public void Setup()
    {
        var types = new List<TypeInfo>
        {
            typeof(TelemetryApiEndpoints).GetTypeInfo(),
        };
        EndpointHelper.InitializeNetworkTargetEndpoints(types);
    }

    [SetUp]
    public void SetUp()
    {
        m_TestInput = null;
        m_CommandLineBuilder = new(new RootCommand("Test root command"));
    }

    [TearDown]
    public void TearDown()
    {
        System.Environment.SetEnvironmentVariable(TestInput.EnvironmentBindingName, null);
    }

    [Test]
    public void CommandUsesOptions_ParseValidOptions()
    {
        var optionArgs = new[]
        {
            k_Command,
            TestInput.ValueIntOption.Aliases.First(),
            k_TestIntValue.ToString(),
            TestInput.ValueStringOption.Aliases.First(),
            k_TestStringValue
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueIntOption,
            TestInput.ValueStringOption
        };
        var parser = BuildCommandWithInputParser(setCommand);
        parser.InvokeAsync(optionArgs);
        Assert.AreEqual(k_TestIntValue, m_TestInput!.IntOptionValue);
        Assert.AreEqual(k_TestStringValue, m_TestInput.StringOptionValue);
    }

    [Test]
    public void CommandUsesOptions_ParseValidArguments()
    {
        var optionArgs = new[]
        {
            k_Command,
            k_TestStringValue,
            k_TestIntValue.ToString()
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueStringArgument,
            TestInput.ValueIntArgument,
        };
        var parser = BuildCommandWithInputParser(setCommand);
        parser.InvokeAsync(optionArgs);
        Assert.AreEqual(k_TestStringValue, m_TestInput!.StringArgValue);
        Assert.AreEqual(k_TestIntValue, m_TestInput.IntArgValue);
    }

    [Test]
    public void InputBinding_HasPriorityOverConfigBinding()
    {
        var optionArgs = new[]
        {
            k_Command,
            k_TestStringValue,
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueStringArgument,
        };

        var parser = BuildCommandWithInputParserWithMockedConfigModule(setCommand);
        parser.InvokeAsync(optionArgs);

        Assert.AreEqual(k_TestStringValue, m_TestInput!.StringArgValue);
    }

    [Test]
    public void ConfigBinding_HasPriorityOverEnvironmentBinding()
    {
        System.Environment.SetEnvironmentVariable(TestInput.EnvironmentBindingName, "value");
        var optionArgs = new[] { k_Command };
        var setCommand = new Command(k_Command, "");

        var parser = BuildCommandWithInputParserWithMockedConfigModule(setCommand);
        parser.InvokeAsync(optionArgs);

        Assert.AreEqual(k_GetConfigMockedReturnValue, m_TestInput!.StringArgValue);
    }

    [Test]
    public void ConfigBindingAttribute_InjectsValueWhenStringEmpty()
    {
        var optionArgs = new[] { k_Command };
        var setCommand = new Command(k_Command, "");

        var parser = BuildCommandWithInputParserWithMockedConfigModule(setCommand);
        parser.InvokeAsync(optionArgs);

        Assert.AreEqual(k_GetConfigMockedReturnValue, m_TestInput!.StringArgValue);
    }

    [Test]
    public void ConfigBindingAttribute_DoesNotInjectWhenStringValueNonEmpty()
    {
        var optionArgs = new[]
        {
            k_Command,
            k_TestStringValue
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueStringArgument,
        };

        var parser = BuildCommandWithInputParserWithMockedConfigModule(setCommand);
        parser.InvokeAsync(optionArgs);

        Assert.AreEqual(k_TestStringValue, m_TestInput!.StringArgValue);
    }

    [Test]
    public void EnvironmentBindingAttribute_InjectsValueWhenStringEmpty()
    {
        System.Environment.SetEnvironmentVariable(TestInput.EnvironmentBindingName, "value");
        var optionArgs = new[] { k_Command };
        var setCommand = new Command(k_Command, "");

        var parser = BuildCommandWithInputParser(setCommand);
        parser.InvokeAsync(optionArgs);

        Assert.AreEqual("value", m_TestInput!.StringArgValue!);
    }

    [Test]
    public void EnvironmentBindingAttribute_DoesNotInjectWhenStringValueNonEmpty()
    {
        System.Environment.SetEnvironmentVariable(TestInput.EnvironmentBindingName, "value");
        var optionArgs = new[]
        {
            k_Command,
            k_TestStringValue
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueStringArgument,
        };

        var parser = BuildCommandWithInputParser(setCommand);
        parser.InvokeAsync(optionArgs);

        Assert.AreEqual(k_TestStringValue, m_TestInput!.StringArgValue!);
    }

    [Test]
    public void CommandUsesOptions_ParseInvalidArguments()
    {
        var options = new[]
        {
            k_Command,
            k_TestStringValue
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueIntArgument
        };
        var parser = BuildCommandWithInputParser(setCommand);
        Assert.Throws<InvalidOperationException>(() => parser.InvokeAsync(options).GetAwaiter().GetResult());
    }

    [Test]
    public void CommandUsesOptions_ParseInvalidOption()
    {
        var optionArgs = new[]
        {
            k_Command,
            TestInput.ValueIntOption.Aliases.First(),
            k_TestStringValue
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueIntOption,
        };
        var parser = BuildCommandWithInputParser(setCommand);
        Assert.Throws<InvalidOperationException>(() => parser.InvokeAsync(optionArgs).GetAwaiter().GetResult());
    }

    [Test]
    public void CommandUsesOptions_ParseLackOfArguments()
    {
        var optionArgs = new[]
        {
            k_Command,
            k_TestStringValue
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueStringArgument,
            TestInput.ValueIntArgument,
        };
        var parser = BuildCommandWithInputParser(setCommand);
        Assert.Throws<InvalidOperationException>(() => parser.InvokeAsync(optionArgs).GetAwaiter().GetResult());
    }

    [Test]
    public void CommandUsesOptions_ParseLackOfOption()
    {
        var optionArgs = new[]
        {
            k_Command,
            TestInput.ValueIntOption.Aliases.First()
        };
        var setCommand = new Command(k_Command, "")
        {
            TestInput.ValueIntOption
        };
        var parser = BuildCommandWithInputParser(setCommand);
        Assert.Throws<InvalidOperationException>(() => parser.InvokeAsync(optionArgs).GetAwaiter().GetResult());
    }

    Parser BuildCommandWithInputParser(Command command)
    {
        m_CommandLineBuilder = m_CommandLineBuilder.UseHost(_ => Host.CreateDefaultBuilder(),
            host =>
            {
                host.ConfigureServices(ConfigurationModule.RegisterServices);
                host.ConfigureServices(serviceCollection => serviceCollection
                    .AddSingleton<ISystemEnvironmentProvider>(new SystemEnvironmentProvider()));
                Mock<IUnityEnvironment> mockUnityEnvironment = new Mock<IUnityEnvironment>();
                host.ConfigureServices(serviceCollection => serviceCollection
                    .AddSingleton(mockUnityEnvironment.Object));
                Mock<IAnalyticEventFactory> mockAnalyticEventFactory = new Mock<IAnalyticEventFactory>();
                host.ConfigureServices(serviceCollection => serviceCollection
                    .AddSingleton(mockAnalyticEventFactory.Object));
            })
            .AddCommandInputParserMiddleware();

        command.SetHandler((TestInput input) => { m_TestInput = input; });
        m_CommandLineBuilder.Command.AddCommand(command);
        return m_CommandLineBuilder.Build();
    }

    Parser BuildCommandWithInputParserWithMockedConfigModule(Command command)
    {
        m_CommandLineBuilder = m_CommandLineBuilder.UseHost(_ => Host.CreateDefaultBuilder(),
                host =>
                {
                    Mock<IConfigurationService> configurationService = new Mock<IConfigurationService>();
                    configurationService.Setup(ex => ex
                        .GetConfigArgumentsAsync(It.IsAny<string>(), CancellationToken.None)).
                        Returns(Task.FromResult<string?>(k_GetConfigMockedReturnValue));

                    host.ConfigureServices(serviceCollection =>
                        serviceCollection.AddSingleton<IConfigurationService>(configurationService.Object));

                    host.ConfigureServices(serviceCollection => serviceCollection
                        .AddSingleton<ISystemEnvironmentProvider>(new SystemEnvironmentProvider()));

                    Mock<IUnityEnvironment> mockUnityEnvironment = new Mock<IUnityEnvironment>();
                    host.ConfigureServices(serviceCollection => serviceCollection
                        .AddSingleton(mockUnityEnvironment.Object));

                    Mock<IAnalyticEventFactory> mockAnalyticEventFactory = new Mock<IAnalyticEventFactory>();
                    host.ConfigureServices(serviceCollection => serviceCollection
                        .AddSingleton(mockAnalyticEventFactory.Object));
                })
            .AddCommandInputParserMiddleware();

        command.SetHandler((TestInput input) => { m_TestInput = input; });
        m_CommandLineBuilder.Command.AddCommand(command);
        return m_CommandLineBuilder.Build();
    }

    [Test]
    public void LoggerCorrectlyInjected()
    {
        var optionArgs = new[]
        {
            k_Command,
        };
        var command = new Command(k_Command, "");
        Logger resultLogger = new Logger();

        m_CommandLineBuilder = m_CommandLineBuilder.UseHost()
            .AddLoggerMiddleware(resultLogger);

        m_CommandLineBuilder.Command.AddCommand(command);
        var parser = m_CommandLineBuilder.Build();

        parser.Invoke(optionArgs);
        Assert.NotNull(resultLogger);
    }

    [Test]
    public void CliServiceCorrectlyInjected()
    {
        IFakeService? fakeService = null;
        var mockServices = new Mock<IServiceTypeList>();
        mockServices.SetupGet(s => s.ServiceTypes).Returns(new List<Type>
        {
            typeof(IFakeService)
        });

        var optionArgs = new[]
        {
            k_Command,
        };
        var command = new Command(k_Command, "");
        command.SetHandler<IFakeService>(FakeServiceHandle);
        void FakeServiceHandle(IFakeService service)
        {
            fakeService = service;
        }

        m_CommandLineBuilder = m_CommandLineBuilder.UseHost(
            host =>
            {
                host.ConfigureServices(RegisterServices);
            }).AddCliServicesMiddleware(mockServices.Object);
        m_CommandLineBuilder.Command.AddCommand(command);

        void RegisterServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IFakeService, FakeService>();
        }

        var parser = m_CommandLineBuilder.Build();
        parser.Invoke(optionArgs);
        Assert.NotNull(fakeService);
    }

    [Test]
    public void IsMemberBoundToInputDefinitionOnBoundPropertySucceeds()
    {
        var member = typeof(TestInput).GetProperty(nameof(TestInput.StringArgValue))!;

        var isBound = ContextBinder.IsMemberBoundToInputDefinition(member);

        Assert.IsTrue(isBound);
    }

    [Test]
    public void IsMemberBoundToInputDefinitionOnBoundFieldSucceeds()
    {
        var member = typeof(TestInput).GetField(nameof(TestInput.StringOptionValue))!;

        var isBound = ContextBinder.IsMemberBoundToInputDefinition(member);

        Assert.IsTrue(isBound);
    }

    [Test]
    public void TryGetSymbolSucceeds()
    {
        var isFound = ContextBinder.TryGetSymbol<Option>(
            typeof(TestInput), nameof(TestInput.ValueIntOption), out var option);

        Assert.IsTrue(isFound);
        Assert.AreSame(TestInput.ValueIntOption, option);
    }

    [Test]
    public void CreateInputFromParseResultSucceeds()
    {
        var parseResult = CreateTestParseResult();
        var inputInstance = new TestInput();
        ContextBinder.SetInputFromParseResult(typeof(TestInput), parseResult, inputInstance);
        var testInput = (TestInput)inputInstance;
        Assert.AreEqual(k_TestIntValue, testInput.IntOptionValue);
    }

    [Test]
    public void GetValueForSymbolSucceeds()
    {
        var parseResult = CreateTestParseResult();

        var symbolValue = ContextBinder.GetValueForSymbol(parseResult, TestInput.ValueIntOption);

        Assert.AreEqual(k_TestIntValue, symbolValue);
    }

    [Test]
    public void SetInputMemberSucceeds()
    {
        var parseResult = CreateTestParseResult();
        var inputType = typeof(TestInput);
        var input = new TestInput();
        var memberInfo = inputType.GetProperty(nameof(TestInput.IntOptionValue))!;

        ContextBinder.SetInputMember(inputType, parseResult, input, memberInfo);

        Assert.AreEqual(k_TestIntValue, input.IntOptionValue);
    }

    static ParseResult CreateTestParseResult()
    {
        var command = new Command(k_Command)
        {
            TestInput.ValueIntOption,
        };
        var optionAlias = TestInput.ValueIntOption.Aliases.First();
        return new Parser(command).Parse($"{k_Command} {optionAlias} {k_TestIntValue}");
    }
}
