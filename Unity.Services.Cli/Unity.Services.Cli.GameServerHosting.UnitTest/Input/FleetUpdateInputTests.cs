using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

[TestFixture]
public class FleetUpdateInputTests
{
    [TestCase(new[]
    {
        FleetUpdateInput.UsageSettingsKey,
        "badjson"
    }, false)]
    [TestCase(new[]
    {
        FleetUpdateInput.UsageSettingsKey,
        ValidUsageSettingsJson
    }, true)]
    public void Validate_WithValidUsageSettings_ReturnsTrue(string[] usageSetting, bool validates)
    {
        Assert.That(FleetUpdateInput.UsageSettingsOption.Parse(usageSetting).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
