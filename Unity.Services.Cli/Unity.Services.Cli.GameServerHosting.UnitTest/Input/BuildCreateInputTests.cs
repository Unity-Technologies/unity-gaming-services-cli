using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

public class BuildCreateInputTests
{
    [TestCase(new[]
    {
        BuildCreateInput.TypeKey,
        "invalid"
    }, false)]
    [TestCase(new[]
    {
        BuildCreateInput.TypeKey,
        "FILEUPLOAD"
    }, true)]
    [TestCase(new[]
    {
        BuildCreateInput.TypeKey,
        "CONTAINER"
    }, true)]
    [TestCase(new[]
    {
        BuildCreateInput.TypeKey,
        "S3"
    }, true)]
    public void Validate_WithValidUUIDInput_ReturnsTrue(string[] buildType, bool validates)
    {
        Assert.That(BuildCreateInput.BuildTypeOption.Parse(buildType).Errors, validates ? Is.Empty : Is.Not.Empty);
    }

    [TestCase(new[]
    {
        BuildCreateInput.OsFamilyKey,
        "invalid"
    }, false)]
    [TestCase(new[]
    {
        BuildCreateInput.OsFamilyKey,
        "LINUX"
    }, true)]
    public void Validate_WithValidOSFamily_ReturnsTrue(string[] osFamily, bool validates)
    {
        Assert.That(BuildCreateInput.BuildOsFamilyOption.Parse(osFamily).Errors, validates ? Is.Empty : Is.Not.Empty);
    }
}
