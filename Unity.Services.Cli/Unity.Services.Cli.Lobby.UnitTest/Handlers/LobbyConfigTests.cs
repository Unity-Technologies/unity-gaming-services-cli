using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;

namespace Unity.Services.Cli.Lobby.UnitTest.Handlers;

[TestFixture]
public class LobbyConfigTests
{
    const string k_TimestampFormat = "yyyy-MM-ddThh:mm:ttZ";
    const string k_DefaultStringSetting = "string-setting";
    const int k_DefaultIntSetting = 1;

    [Test]
    public void TryParse_SucceedsWithValidConfig()
    {
        var configResponse = NewDefaultConfig();
        var json = JsonConvert.SerializeObject(
            configResponse,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

        var success = LobbyConfig.TryParse(json, out var lobbyConfig);
        Assert.True(success);
        Assert.NotNull(lobbyConfig);
        Assert.AreEqual(lobbyConfig?.Id, configResponse.Configs.First().Id);
        if (lobbyConfig != null)
        {
            Assert.AreEqual(2, lobbyConfig.Config.Count);
            Assert.True(lobbyConfig.Config.ContainsKey(nameof(MockLobbyConfig.StringSetting)));
            Assert.AreEqual(
                k_DefaultStringSetting,
                lobbyConfig.Config.GetValue(nameof(MockLobbyConfig.StringSetting))!.Value<string>());
            Assert.True(lobbyConfig.Config.ContainsKey(nameof(MockLobbyConfig.IntSetting)));
            Assert.AreEqual(
                k_DefaultIntSetting,
                lobbyConfig.Config.GetValue(nameof(MockLobbyConfig.IntSetting))!.Value<int>());
        }
    }

    [Test]
    public void TryParse_FailsWithNoLobbyConfigValue()
    {
        var configResponse = NewDefaultConfig(includeMockConfig: false);
        var json = JsonConvert.SerializeObject(
            configResponse,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

        var success = LobbyConfig.TryParse(json, out var lobbyConfig);
        Assert.False(success);
        Assert.Null(lobbyConfig);
    }

    [Test]
    public void TryParse_FailsWithNoConfigs()
    {
        var configResponse = new List<RemoteConfigValue>();
        var json = JsonConvert.SerializeObject(
            configResponse,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

        var success = LobbyConfig.TryParse(json, out var lobbyConfig);
        Assert.False(success);
        Assert.Null(lobbyConfig);
    }

    static RemoteConfigResponse NewDefaultConfig(bool includeMockConfig = true)
    {
        var mockConfigJson = includeMockConfig ? JsonConvert.SerializeObject(
             new MockLobbyConfig(k_DefaultStringSetting, k_DefaultIntSetting)) : "{}";

        var now = DateTime.Now.ToString(k_TimestampFormat);
        var config = new RemoteConfigValue
        {
            ProjectId = Guid.NewGuid().ToString(),
            EnvironmentId = Guid.NewGuid().ToString(),
            Id = Guid.NewGuid().ToString(),
            Type = LobbyConstants.ConfigType,
            CreatedAt = now,
            UpdatedAt = now,
            Value = new List<LobbyConfigValue>
            {
                new LobbyConfigValue{
                    Key = LobbyConstants.ConfigKey,
                    Type = RemoteConfig.Types.ValueType.Json.ToString().ToLower(),
                    SchemaId = LobbyConstants.ConfigType,
                    Value = JObject.Parse(mockConfigJson)
                }
            }
        };

        return new RemoteConfigResponse
        {
            Configs = new List<RemoteConfigValue>
            {
                config
            }
        };
    }

    class MockLobbyConfig
    {
        public MockLobbyConfig(string stringSetting, int intSetting)
        {
            StringSetting = stringSetting;
            IntSetting = intSetting;
        }

        public string StringSetting { get; set; }
        public int IntSetting { get; set; }
    }
}
