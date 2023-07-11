using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class BuildIdInputTests
{
    [TestCase("alphaString", false)]
    [TestCase("123456", true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string buildId, bool validates)
    {
        Assert.That(BuildIdInput.BuildIdArgument.Parse(buildId).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
