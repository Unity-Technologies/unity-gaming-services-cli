using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class ServerListInputTests
{
    [TestCase(InvalidUuid, false)]
    [TestCase(ValidFleetId, true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string id, bool validates)
    {
        var arg = new[]
        {
            ServerListInput.FleetIdKey,
            id
        };
        Assert.That(ServerListInput.FleetIdOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase("invalid", false)]
    [TestCase("1", true)]
    public void Validate_WithValidBuildConfigurationIdInput_ReturnsTrue(string id, bool validates)
    {
        var arg = new[]
        {
            ServerListInput.BuildConfigurationIdKey,
            id
        };
        Assert.That(ServerListInput.BuildConfigurationIdOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase("invalid", false)]
    [TestCase("ONLINE", true)]
    public void Validate_WithValidStatusInput_ReturnsTrue(string status, bool validates)
    {
        var arg = new[]
        {
            ServerListInput.StatusKey,
            status
        };
        Assert.That(ServerListInput.StatusOption.Parse(arg).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
