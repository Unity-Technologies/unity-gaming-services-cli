using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;


namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

[TestFixture]
public class FleetCreateInputTests
{
    [TestCase(new[]
    {
        FleetCreateInput.RegionsKey,
        InvalidUuid
    }, false)]
    [TestCase(new[]
    {
        FleetCreateInput.RegionsKey,
        ValidRegionId
    }, true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string[] regions, bool validates)
    {
        Assert.That(FleetCreateInput.FleetRegionsOption.Parse(regions).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase(new[]
    {
        FleetCreateInput.OsFamilyKey,
        "invalid"
    }, false)]
    [TestCase(new[]
    {
        FleetCreateInput.OsFamilyKey,
        "LINUX"
    }, true)]
    public void Validate_WithValidOSFamily_ReturnsTrue(string[] osFamily, bool validates)
    {
        Assert.That(FleetCreateInput.FleetOsFamilyOption.Parse(osFamily).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
