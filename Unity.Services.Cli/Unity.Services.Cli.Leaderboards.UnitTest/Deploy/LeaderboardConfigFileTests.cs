using NUnit.Framework;
using Unity.Services.Cli.Leaderboards.Deploy;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
public class LeaderboardConfigFileTests
{
    [Test]
    public void HelpBodyText_IsDeterministic()
    {
        var template = new LeaderboardConfigFile();
        var first = template.HelpBodyText;
        var second = template.HelpBodyText;
        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void HelpBodyText_ContainsFixedUtcDate()
    {
        var template = new LeaderboardConfigFile();
        Assert.That(template.HelpBodyText, Does.Contain("2099-01-01T00:00:00Z"));
    }

    [Test]
    public void HelpBodyText_DiffersFromFileBodyText()
    {
        var template = new LeaderboardConfigFile();
        Assert.That(template.HelpBodyText, Is.Not.EqualTo(template.FileBodyText));
    }
}
