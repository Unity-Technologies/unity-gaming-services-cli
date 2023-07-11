using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class BuildConfigurationInputTests
{
    [TestCase(InvalidUuid, false)]
    [TestCase(ValidFleetId, true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string fleetId, bool validates)
    {
        var arg = new[]
        {
            BuildConfigurationListInput.FleetIdKey,
            fleetId,
        };
        Assert.That(BuildConfigurationListInput.FleetIdOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
