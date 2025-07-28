using Newtonsoft.Json;

namespace Unity.Services.Cli.GameServerHosting.Types;

public class LocalFleetSpecificationV5
{
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("labels")] public Dictionary<string, string>? Labels { get; set; }
}
