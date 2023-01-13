using System.CommandLine;
using NUnit.Framework;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class CommandModuleTests
{
    [Test]
    public void GetCommandsForCliRootReturnsEmptyArrayByDefault()
    {
        ICommandModule module = new DummyModule();

        var commandsForCliRoot = module.GetCommandsForCliRoot();

        Assert.That(commandsForCliRoot, Is.Empty);
    }

    [Test]
    public void GetCommandsForCliRootReturnsArrayWithRootIfNotNull()
    {
        var root = new Command("root");
        ICommandModule module = new DummyModule
        {
            ModuleRootCommand = root,
        };

        var commandsForCliRoot = module.GetCommandsForCliRoot();

        Assert.That(commandsForCliRoot, Contains.Item(root));
    }
}
