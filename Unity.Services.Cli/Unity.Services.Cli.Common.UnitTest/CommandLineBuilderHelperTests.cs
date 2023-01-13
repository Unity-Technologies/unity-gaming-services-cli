using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class CommandLineBuilderHelperTests
{
    CommandLineBuilder? m_CommandLineBuilder;

    static IEnumerable<string> s_OptionAlias =
        CommonInput.QuietOption.Aliases.Concat(CommonInput.JsonOutputOption.Aliases);

    [SetUp]
    public void SetUp()
    {
        m_CommandLineBuilder = new(new RootCommand("Test root command"));
    }

    [TestCaseSource(nameof(s_OptionAlias))]
    public void AddGlobalCommonOptions_ParseValidOption(string alias)
    {
        var isQuiet = CommonInput.QuietOption.Aliases.Contains(alias);
        var isJson = CommonInput.JsonOutputOption.Aliases.Contains(alias);
        var parser = m_CommandLineBuilder!.UseDefaults().AddGlobalCommonOptions().Build();
        var result = parser.Parse(alias);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(isQuiet, result.GetValueForOption(CommonInput.QuietOption));
        Assert.AreEqual(isJson, result.GetValueForOption(CommonInput.JsonOutputOption));
    }

    /// <remarks>
    /// Use <see cref="DummyModule"/> instead of mocking to benefit from default
    /// implementation of <see cref="ICommandModule.GetCommandsForCliRoot()"/>.
    /// </remarks>
    [Test]
    public void AddModuleWithRootCommandAddsItToBuilderRootCommand()
    {
        var moduleRoot = new Command("module", "this is a module command");
        var module = new DummyModule
        {
            ModuleRootCommand = moduleRoot,
        };

        m_CommandLineBuilder!.AddModule(module);

        Assert.That(m_CommandLineBuilder!.Command.Children, Contains.Item(moduleRoot));
    }

    [Test]
    public void AddModuleWithNullRootCommandDoesNothing()
    {
        Command? moduleRoot = null;
        Mock<ICommandModule> mockCommandModule = new();
        mockCommandModule.SetupGet(m => m.ModuleRootCommand)
            .Returns(moduleRoot);

        m_CommandLineBuilder!.AddModule(mockCommandModule.Object);

        Assert.That(m_CommandLineBuilder!.Command.Children, Is.Empty);
    }

    [Test]
    public void AddModuleWithEmptyCommandsForCliRootDoesNothing()
    {
        Mock<ICommandModule> mockCommandModule = new();
        mockCommandModule.Setup(m => m.GetCommandsForCliRoot())
            .Returns(Array.Empty<Command>());

        m_CommandLineBuilder!.AddModule(mockCommandModule.Object);

        Assert.That(m_CommandLineBuilder!.Command.Children, Is.Empty);
    }

    [Test]
    public void AddModuleWithCommandsForCliRootDoesAddThemToBuilderRootCommand()
    {
        var dummy = new Command("dummy");
        var foo = new Command("foo");
        var commandsForCliRoot = new List<Command>
        {
            dummy,
            foo,
        };
        Mock<ICommandModule> mockCommandModule = new();
        mockCommandModule.Setup(m => m.GetCommandsForCliRoot())
            .Returns(commandsForCliRoot);

        m_CommandLineBuilder!.AddModule(mockCommandModule.Object);

        Assert.That(m_CommandLineBuilder!.Command.Children, Contains.Item(dummy));
        Assert.That(m_CommandLineBuilder!.Command.Children, Contains.Item(foo));
    }
}
