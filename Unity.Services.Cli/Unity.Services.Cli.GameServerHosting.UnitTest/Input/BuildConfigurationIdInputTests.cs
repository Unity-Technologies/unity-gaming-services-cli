using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class BuildConfigurationIdInputTests
{
    [TestCase("alphaString", false)]
    [TestCase("123456", true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string buildConfiguratinId, bool validates)
    {
        Assert.That(BuildConfigurationIdInput.BuildConfigurationIdArgument.Parse(buildConfiguratinId).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
