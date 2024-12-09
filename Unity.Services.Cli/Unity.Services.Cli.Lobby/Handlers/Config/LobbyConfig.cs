using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Lobby.Handlers.Config;

public class LobbyConfig
{
    public string Id { get; set; }

    public string SchemaId { get; set; }

    public JObject Config { get; set; }

    public LobbyConfig()
    {
        Id = "";
        SchemaId = "";
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
            throw new CliException(
                $"There is no lobby configuration available in this environment.",
                ExitCode.HandledError);
        }

        var lobbyConfig = configResponse.Configs.First(c => c.Type == LobbyConstants.ConfigType);
        var lobbyConfigValue = lobbyConfig.Value.First(c => c.Key == LobbyConstants.ConfigKey);

        var lobbyConfigData = lobbyConfigValue.Value;
        if (lobbyConfigData == null || lobbyConfigData.Count == 0)
        {
            throw new CliException(
                $"There is no lobby configuration available in this environment.",
                ExitCode.HandledError);
        }

        return new LobbyConfig
        {
            Id = lobbyConfig.Id,
            SchemaId = lobbyConfigValue.SchemaId,
            Config = lobbyConfigData
        };
    }

    public static LobbyConfigValue ParseValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(nameof(value));
        }

        UpdateConfigRequest updateConfigRequest;
        try
        {
            updateConfigRequest = JsonConvert.DeserializeObject<UpdateConfigRequest>(value);
            if (updateConfigRequest.Type == null || updateConfigRequest.Value.Count == 0)
            {
                throw new CliException(
                    $"There is no configuration in input value.",
                    ExitCode.HandledError);
            }
        }
        catch (JsonReaderException)
        {
            throw new CliException(
                $"The configuration input value is invalid.",
                ExitCode.HandledError);
        }

        var lobbyConfigValue = updateConfigRequest.Value.First(c => c.Key == LobbyConstants.ConfigKey);

        var lobbyConfigData = lobbyConfigValue.Value;
        if (lobbyConfigData == null || lobbyConfigData.Count == 0)
        {
            throw new CliException(
                $"There is no lobby configuration in input value.",
                ExitCode.HandledError);
        }

        return lobbyConfigValue;
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
    public List<RemoteConfigValue> Configs { get; set; }
}

public struct UpdateConfigRequest
{
    public string Type { get; set; }
    public List<LobbyConfigValue> Value { get; set; }
}
public struct RemoteConfigValue
{
    public string ProjectId { get; set; }
    public string EnvironmentId { get; set; }
    public string Type { get; set; }
    public string Id { get; set; }
    public string Version { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }

    public List<LobbyConfigValue> Value { get; set; }
}

public struct LobbyConfigValue
{
    public string Key { get; set; }
    public string Type { get; set; }
    public string SchemaId { get; set; }
    public JObject Value { get; set; }
}
