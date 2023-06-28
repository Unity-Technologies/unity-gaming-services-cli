using System.CommandLine;
using System.CommandLine.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Export.Input;
using Unity.Services.Cli.Authoring.Import.Input;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Lobby.Input;
using Unity.Services.Cli.Lobby.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.TestUtils;

namespace Unity.Services.Cli.Lobby.UnitTest;

[TestFixture]
class LobbyModuleTests
{
    struct CommandTestCase
    {
        public CommandTestCase(string name, List<Argument> arguments, List<Option> options, List<CommandTestCase>? subcommands = default)
        {
            Name = name;
            Arguments = arguments;
            Options = options;
            Subcommands = subcommands ?? new List<CommandTestCase>();
        }

        public string Name { get; }

        public List<Argument> Arguments { get; }

        public List<Option> Options { get; }

        public List<CommandTestCase> Subcommands { get; }
    }

    static readonly LobbyModule k_LobbyModule = new();


    static readonly List<Option> ServiceIdOptionList = new()
    {
        CommonLobbyInput.ServiceIdOption,
        CommonInput.CloudProjectIdOption,
        CommonInput.EnvironmentNameOption
    };

    static readonly IEnumerable<CommandTestCase> k_CommandTestCases = new[]
    {
        new CommandTestCase("bulk-update",
            new List<Argument>() { CommonLobbyInput.LobbyIdArgument, RequiredBodyInput.RequestBodyArgument },
            new List<Option>(ServiceIdOptionList)),
        new CommandTestCase("create",
            new List<Argument>() { RequiredBodyInput.RequestBodyArgument },
            new List<Option>(ServiceIdOptionList) { CommonLobbyInput.PlayerIdOption }),
        new CommandTestCase("delete",
            new List<Argument>() { CommonLobbyInput.LobbyIdArgument },
            new List<Option>(ServiceIdOptionList) { CommonLobbyInput.PlayerIdOption }),
        new CommandTestCase("get-hosted",
            new List<Argument>(),
            new List<Option>(ServiceIdOptionList) { CommonLobbyInput.PlayerIdOption }),
        new CommandTestCase("get-joined",
            new List<Argument>() { PlayerInput.PlayerIdArgument },
            new List<Option>(ServiceIdOptionList)),
        new CommandTestCase("get",
            new List<Argument>() { CommonLobbyInput.LobbyIdArgument },
            new List<Option>(ServiceIdOptionList) { CommonLobbyInput.PlayerIdOption }),
        new CommandTestCase("heartbeat",
            new List<Argument>() { CommonLobbyInput.LobbyIdArgument },
            new List<Option>(ServiceIdOptionList) { CommonLobbyInput.PlayerIdOption }),
        new CommandTestCase("join",
            new List<Argument>() { CommonLobbyInput.PlayerDetailsArgument },
            new List<Option>(ServiceIdOptionList) { JoinInput.LobbyIdOption, JoinInput.LobbyCodeOption }),
        new CommandTestCase("player",
            new List<Argument>(),
            new List<Option>(),
            new List<CommandTestCase>() {
                new CommandTestCase("update",
                    new List<Argument>() { CommonLobbyInput.LobbyIdArgument, PlayerInput.PlayerIdArgument, RequiredBodyInput.RequestBodyArgument },
                    new List<Option>(ServiceIdOptionList)),
                new CommandTestCase("remove",
                    new List<Argument>() { CommonLobbyInput.LobbyIdArgument, PlayerInput.PlayerIdArgument },
                    new List<Option>(ServiceIdOptionList)),
            }),
        new CommandTestCase("query",
            new List<Argument>(),
            new List<Option>(ServiceIdOptionList) { CommonLobbyInput.PlayerIdOption, LobbyBodyInput.JsonFileOrBodyOption }),
        new CommandTestCase("quickjoin",
            new List<Argument>() { CommonLobbyInput.QueryFilterArgument, LobbyBodyInput.PlayerDetailsArgument },
            new List<Option>(ServiceIdOptionList)),
        new CommandTestCase("reconnect",
            new List<Argument>() { CommonLobbyInput.LobbyIdArgument, PlayerInput.PlayerIdArgument },
            new List<Option>(ServiceIdOptionList)),
        new CommandTestCase("request-token",
            new List<Argument>() { CommonLobbyInput.LobbyIdArgument, PlayerInput.PlayerIdArgument, LobbyTokenInput.TokenTypeArgument },
            new List<Option>(ServiceIdOptionList)),
        new CommandTestCase("update",
            new List<Argument>() { CommonLobbyInput.LobbyIdArgument, RequiredBodyInput.RequestBodyArgument },
            new List<Option>(ServiceIdOptionList) { CommonLobbyInput.PlayerIdOption }),
        new CommandTestCase("config",
            new List<Argument>(),
            new List<Option>(),
            new List<CommandTestCase>() {
                new CommandTestCase("get",
                    new List<Argument>(),
                    new List<Option>(){ CommonInput.CloudProjectIdOption, CommonInput.EnvironmentNameOption }),
                new CommandTestCase("update",
                    new List<Argument>() { LobbyConfigUpdateInput.ConfigIdArgument, RequiredBodyInput.RequestBodyArgument },
                    new List<Option>(){ CommonInput.CloudProjectIdOption }),
            }
        ),
        new CommandTestCase("import",
            new List<Argument>() { ImportInput.InputDirectoryArgument, ImportInput.FileNameArgument },
            new List<Option>() { CommonInput.CloudProjectIdOption, CommonInput.EnvironmentNameOption, ImportInput.DryRunOption, ImportInput.ReconcileOption }),
        new CommandTestCase("export",
            new List<Argument>() { ExportInput.OutputDirectoryArgument, ExportInput.FileNameArgument },
            new List<Option>() { CommonInput.CloudProjectIdOption, CommonInput.EnvironmentNameOption, ImportInput.DryRunOption }),
    };

    [Test]
    public void BuildCommands_CreateCommands()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_LobbyModule);
        TestsHelper.AssertContainsCommand(commandLineBuilder.Command, k_LobbyModule.ModuleRootCommand.Name, out var rootCommand);
        Assert.AreEqual(k_LobbyModule.ModuleRootCommand, rootCommand);

        var expectedCommandNames = k_CommandTestCases.Select(t => t.Name);
        var actualCommandNames = rootCommand.AsEnumerable().Select(c => c.Name).ToList();
        CollectionAssert.AreEqual(expectedCommandNames, actualCommandNames);
    }

    [Test]
    public void BuildCommands_CreateAliases()
    {
        var commandLineBuilder = new CommandLineBuilder();
        commandLineBuilder.AddModule(k_LobbyModule);
        Assert.IsTrue(k_LobbyModule.ModuleRootCommand.Aliases.Contains("lobby"));
    }

    [Test]
    public void BuildCommands_CommandsHaveExpectedArgumentsOptionsAndSubcommands()
    {
        foreach (var testCase in k_CommandTestCases)
        {
            VerifyCommand(k_LobbyModule.ModuleRootCommand, testCase);
        }
    }

    [Test]
    public void ConfigureLobbyRegistersExpectedServices()
    {
        var services = new List<ServiceDescriptor>
        {
            ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object),
        };
        var hostBuilder = TestsHelper.CreateAndSetupMockHostBuilder(services);
        hostBuilder.ConfigureServices(LobbyModule.RegisterServices);

        Assert.AreEqual(4, services.Count);
        TestsHelper.AssertHasServiceSingleton<ILobbyService, LobbyService>(services);
    }

    static void VerifyCommand(Command parentCommand, CommandTestCase testCase)
    {
        TestsHelper.AssertContainsCommand(parentCommand, testCase.Name, out var command);
        CollectionAssert.AreEqual(testCase.Arguments, command.Arguments, $"Expected Arguments do not match for \"{parentCommand.Name} {testCase.Name}\"");
        CollectionAssert.AreEqual(testCase.Options, command.Options, $"Expected Options do not match for \"{parentCommand.Name} {testCase.Name}\"");
        Assert.AreEqual(testCase.Subcommands.Count, command.Subcommands.Count, $"Expected Subcommands count does not match for \"{parentCommand.Name} {testCase.Name}\"");
        foreach (var subcommand in testCase.Subcommands)
        {
            VerifyCommand(command, subcommand);
        }
    }
}
