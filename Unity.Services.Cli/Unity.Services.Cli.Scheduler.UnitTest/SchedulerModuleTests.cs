using System.CommandLine;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Scheduler.Input;

namespace Unity.Services.Cli.Scheduler.UnitTest;

[TestFixture]
class SchedulerModuleTests
{
    static readonly SchedulerModule k_Module = new();

    [TestCase("activate")]
    [TestCase("deactivate")]
    public void StatusCommands_AreRegisteredWithRequiredInputs(string name)
    {
        var command = k_Module.ModuleRootCommand!.Subcommands.FirstOrDefault(c => c.Name == name);

        Assert.That(command, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(command!.Arguments, Does.Contain(UpdateScheduleStatusInput.ScheduleIdArgument));
            Assert.That(command.Options, Does.Contain(CommonInput.CloudProjectIdOption));
            Assert.That(command.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        });
    }

}
