using System.CommandLine;
using NUnit.Framework;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Triggers.Input;

namespace Unity.Services.Cli.Triggers.UnitTest;

[TestFixture]
class TriggersModuleTests
{
    static readonly TriggersModule k_Module = new();

    [Test]
    public void ValidateRootCommand()
    {
        Assert.That(k_Module.ModuleRootCommand, Is.Not.Null);
        Assert.That(k_Module.ModuleRootCommand!.Name, Is.EqualTo("triggers"));
        Assert.That(k_Module.ModuleRootCommand.Aliases, Does.Contain("tr"));
    }

    [TestCase("activate")]
    [TestCase("deactivate")]
    public void StatusCommands_AreRegisteredWithRequiredInputs(string name)
    {
        var command = k_Module.ModuleRootCommand!.Subcommands.FirstOrDefault(c => c.Name == name);

        Assert.That(command, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(command!.Arguments, Does.Contain(UpdateTriggerStatusInput.TriggerIdArgument));
            Assert.That(command.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(command.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        });
    }
}
