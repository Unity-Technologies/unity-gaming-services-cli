using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

[TestFixture]
public class SimulatorInputTests
{
    [Test]
    public void Validate_ManageProcessArgument()
    {
        var args = new[]
        {
            SimulatorInput.ManageProcessOption.Aliases.Last(),
            "./gameserver.exe start"
        };

        Assert.That(SimulatorInput.ManageProcessOption.Parse(args).Errors, Is.Empty);
    }
}
