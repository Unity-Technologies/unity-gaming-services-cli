using Newtonsoft.Json;

namespace Unity.Services.Cli.Economy.Model;

class Resource
{
    [JsonProperty("id")] [JsonRequired] public string Id;
    [JsonProperty("name")] [JsonRequired] public string Name;
    [JsonProperty("type")] [JsonRequired] public string Type;
    [JsonProperty("customData")] public object? CustomData;

    public Resource(string id, string name, string type, object? customData)
    {
        Id = id;
        Name = name;
        Type = type;
        CustomData = customData;
    }
}
