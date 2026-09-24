using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Lobby.Deploy;
using Unity.Services.Cli.Lobby.Handlers;

namespace Unity.Services.Cli.Lobby.UnitTest.Deploy;

[TestFixture]
class LobbyConfigFileContentTests
{
    [Test]
    public void Constructor_FromJObject_SetsAllProperties()
    {
        var config = new JObject
        {
            [LobbyConstants.ActiveLifespanSecondsKey] = 60,
            [LobbyConstants.DisconnectRemovalTimeSecondsKey] = 240,
            [LobbyConstants.DisconnectHostMigrationTimeSecondsKey] = 20,
            [LobbyConstants.PlayerSlotsKey] = new JObject
            {
                [LobbyConstants.PlayerSlotsMinimumKey] = 2,
                [LobbyConstants.PlayerSlotsMaximumKey] = 100
            },
            [LobbyConstants.SocialProfilesEnabledKey] = true
        };

        var content = new LobbyConfigFileContent("schema-url", "schema-id", config);

        Assert.That(content.Schema, Is.EqualTo("schema-url"));
        Assert.That(content.SchemaId, Is.EqualTo("schema-id"));
        Assert.That(content.ActiveLifespanSeconds, Is.EqualTo(60));
        Assert.That(content.DisconnectRemovalTimeSeconds, Is.EqualTo(240));
        Assert.That(content.DisconnectHostMigrationTimeSeconds, Is.EqualTo(20));
        Assert.That(content.PlayerSlots.Minimum, Is.EqualTo(2));
        Assert.That(content.PlayerSlots.Maximum, Is.EqualTo(100));
        Assert.That(content.SocialProfilesEnabled, Is.True);
    }

    [Test]
    public void ToConfigJObject_RoundTrips_WithSocialProfilesEnabled()
    {
        var original = new JObject
        {
            [LobbyConstants.ActiveLifespanSecondsKey] = 30,
            [LobbyConstants.DisconnectRemovalTimeSecondsKey] = 120,
            [LobbyConstants.DisconnectHostMigrationTimeSecondsKey] = 10,
            [LobbyConstants.PlayerSlotsKey] = new JObject
            {
                [LobbyConstants.PlayerSlotsMinimumKey] = 1,
                [LobbyConstants.PlayerSlotsMaximumKey] = 150
            },
            [LobbyConstants.SocialProfilesEnabledKey] = false
        };

        var content = new LobbyConfigFileContent("s", "id", original);
        var result = content.ToConfigJObject();

        Assert.That(result.Value<int>(LobbyConstants.ActiveLifespanSecondsKey), Is.EqualTo(30));
        Assert.That(result.Value<int>(LobbyConstants.DisconnectRemovalTimeSecondsKey), Is.EqualTo(120));
        Assert.That(result.Value<int>(LobbyConstants.DisconnectHostMigrationTimeSecondsKey), Is.EqualTo(10));
        Assert.That(result[LobbyConstants.PlayerSlotsKey]!.Value<int>(LobbyConstants.PlayerSlotsMinimumKey), Is.EqualTo(1));
        Assert.That(result[LobbyConstants.PlayerSlotsKey]!.Value<int>(LobbyConstants.PlayerSlotsMaximumKey), Is.EqualTo(150));
        Assert.That(result.Value<bool>(LobbyConstants.SocialProfilesEnabledKey), Is.False);
    }

    [Test]
    public void ToConfigJObject_OmitsSocialProfiles_WhenNull()
    {
        var original = new JObject
        {
            [LobbyConstants.ActiveLifespanSecondsKey] = 30,
            [LobbyConstants.DisconnectRemovalTimeSecondsKey] = 120,
            [LobbyConstants.DisconnectHostMigrationTimeSecondsKey] = 10,
            [LobbyConstants.PlayerSlotsKey] = new JObject
            {
                [LobbyConstants.PlayerSlotsMinimumKey] = 1,
                [LobbyConstants.PlayerSlotsMaximumKey] = 150
            }
        };

        var content = new LobbyConfigFileContent("s", "id", original);
        var result = content.ToConfigJObject();

        Assert.That(content.SocialProfilesEnabled, Is.Null);
        Assert.That(result.ContainsKey(LobbyConstants.SocialProfilesEnabledKey), Is.False);
    }
}
