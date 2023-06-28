using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Lobby.Handlers.ImportExport;

public class LobbyConfig
{
    public string Id { get; set; }

    public JObject Config { get; set; }

    public LobbyConfig()
    {
        Id = "";
        Config = new JObject();
    }

    public static LobbyConfig Parse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new ArgumentNullException(nameof(response));
        }

        var configResponse = JsonConvert.DeserializeObject<RemoteConfigResponse>(response);
        if (configResponse.Configs == null || configResponse.Configs.Count == 0)
        {
            throw new CliException($"There is no lobby configuration available in this environment.", ExitCode.HandledError);
        }

        var lobbyConfig = configResponse.Configs.First(c => c.Type == LobbyConstants.ConfigType);
        var lobbyConfigValue = lobbyConfig.Value.First(c => c.Key == LobbyConstants.ConfigKey);

        var lobbyConfigData = lobbyConfigValue.Value;
        if (lobbyConfigData == null || lobbyConfigData.Count == 0)
        {
            throw new CliException($"There is no lobby configuration available in this environment.", ExitCode.HandledError);
        }

        return new LobbyConfig
        {
            Id = lobbyConfig.Id,
            Config = lobbyConfigData
        };
    }

    public static bool TryParse(string response, out LobbyConfig? config)
    {
        try
        {
            config = Parse(response);
            return true;
        }
        catch
        {
            config = null;
            return false;
        }
    }
}


public struct RemoteConfigResponse
{
    public List<Config> Configs { get; set; }

    public struct Config
    {
        public string ProjectId { get; set; }
        public string EnvironmentId { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }

        public List<ConfigValue> Value { get; set; }
    }

    public struct ConfigValue
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string SchemaId { get; set; }
        public JObject Value { get; set; }
    }
}
