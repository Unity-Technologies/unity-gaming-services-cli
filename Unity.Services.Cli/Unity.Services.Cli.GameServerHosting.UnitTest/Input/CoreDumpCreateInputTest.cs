using System.CommandLine;
using Unity.Services.Cli.GameServerHosting.Input;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Input;

[TestFixture]
[TestOf(typeof(CoreDumpCreateInput))]
public class CoreDumpCreateInputTest
{
    [TestCase("disabled", true)]
    [TestCase("enabled", true)]
    [TestCase("unavailable", false)]
    public void ValidateStateOption(string state, bool valid)
    {
        var args = new[]
        {
            CoreDumpCreateInput.StateOption.Aliases.First(),
            state
        };
        Assert.That(CoreDumpCreateInput.StateOption.Parse(args).Errors, valid ? Is.Empty : Is.Not.Empty);
    }
}
