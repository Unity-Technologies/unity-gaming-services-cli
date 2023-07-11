using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class ServerIdInputTests
{
    [TestCase("nan", false)]
    [TestCase("666", true)]
    public void Validate_WithValidServerIdInput_ReturnsTrue(string serverId, bool validates)
    {
        Assert.That(ServerIdInput.ServerIdArgument.Parse(serverId).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
