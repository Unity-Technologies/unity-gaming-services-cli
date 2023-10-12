using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class MachineListInputTests
{
    [TestCase(InvalidUuid, false)]
    [TestCase(ValidFleetId, true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string id, bool validates)
    {
        var arg = new[]
        {
            MachineListInput.FleetIdKey,
            id
        };
        Assert.That(MachineListInput.FleetIdOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase("invalid", false)]
    [TestCase("CLOUD", true)]
    public void Validate_WithValidHardwareTypeInput_ReturnsTrue(string id, bool validates)
    {
        var arg = new[]
        {
            MachineListInput.HardwareTypeKey,
            id
        };
        Assert.That(MachineListInput.HardwareTypeOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase("invalid", false)]
    [TestCase("ONLINE", true)]
    public void Validate_WithValidStatusInput_ReturnsTrue(string status, bool validates)
    {
        var arg = new[]
        {
            MachineListInput.StatusKey,
            status
        };
        Assert.That(MachineListInput.StatusOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
