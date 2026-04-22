using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Services.Cli.GameServerHosting.Types;

public class ServerListV5
{
    [JsonProperty("results")]
    public ServerInfoV5[]? Results { get; set; }
}

public class ServerInfoV5
{
    [JsonProperty("connections")]
    public ServerInfoConnectionV5[]? Connections { get; set; }
    [JsonProperty("fleetId")]
    public string? FleetId { get; set; }
    [JsonProperty("state")]
    public string? State { get; set; }
}

public class ServerInfoConnectionV5
{
    [JsonProperty("host")]
    public string? Host { get; set; }
    [JsonProperty("port")]
    public int Port { get; set; }
    [JsonProperty("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();
}
