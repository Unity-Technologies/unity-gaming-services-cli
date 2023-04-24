using NUnit.Framework;
using Unity.Services.Cli.Leaderboards.Model;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Model;

[TestFixture]
class LeaderboardConfigsResponseOutputTests
{
    UpdatedLeaderboardConfig m_UpdatedLeaderboardConfig = new (
        "",
        ""
        );

    [SetUp]
    public void SetUp()
    {
        const string id = "id";
        const string name = "Test";
        m_UpdatedLeaderboardConfig = new UpdatedLeaderboardConfig(id, name);
    }

    [Test]
    public void OutputScriptToStringReturnFormattedString()
    {
        var outputScript = new LeaderboardConfigInner(m_UpdatedLeaderboardConfig);
        var outputScriptString = outputScript.ToString();
        var expectedString = @"id: id
name: Test
".Replace("\r\n", "\n")
            .Replace("\n", System.Environment.NewLine);
        Assert.AreEqual(expectedString, outputScriptString);
    }
}


