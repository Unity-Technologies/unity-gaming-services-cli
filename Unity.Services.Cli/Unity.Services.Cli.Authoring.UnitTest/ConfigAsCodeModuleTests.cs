using System.Linq;
using NUnit.Framework;

namespace Unity.Services.Cli.Authoring.UnitTest;

[TestFixture]
class ConfigAsCodeModuleTests
{
    static readonly ConfigAsCodeModule k_Module = new();

    [Test]
    public void ModuleRootCommand_HasCorrectName()
    {
        Assert.That(k_Module.ModuleRootCommand!.Name, Is.EqualTo("config-as-code"));
    }

    [Test]
    public void ModuleRootCommand_HasCacAlias()
    {
        Assert.That(k_Module.ModuleRootCommand!.Aliases, Does.Contain("cac"));
    }

    [Test]
    public void ModuleRootCommand_HasNewFileSubcommand()
    {
        var newFileCommand = k_Module.ModuleRootCommand!.Subcommands
            .FirstOrDefault(c => c.Name == "new-file");
        Assert.That(newFileCommand, Is.Not.Null);
    }
}
