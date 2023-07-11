using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class FleetIdInputTests
{
    [TestCase(InvalidUuid, false)]
    [TestCase(ValidFleetId,true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string regions, bool validates)
    {
        Assert.That(FleetIdInput.FleetIdArgument.Parse(regions).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
