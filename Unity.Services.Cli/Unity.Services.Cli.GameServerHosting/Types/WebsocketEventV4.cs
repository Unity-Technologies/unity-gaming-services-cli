using Newtonsoft.Json;

namespace Unity.Services.Cli.GameServerHosting.Types;

// Message example: {"result":{"channel":"server#1","data":{"data":{"EventType":"AllocateEventType","EventID":"7d9881b9-1619-48c2-8289-7fcbcfd308c2","ServerID":1883262176879104,"AllocationID":"cd3b28de-88dd-41c8-bc8c-e6f9c2015c6c"}}}}
public class WebsocketEventV4
{
    [JsonProperty("result")]
    public WebsocketEventV4Result? Result { get; set; }
}

public class WebsocketEventV4Result
{
    [JsonProperty("channel")]
    public string? Channel { get; set; }

    [JsonProperty("data")]
    public WebsocketEventV4Data? Data { get; set; }
}

public class WebsocketEventV4Data
{
    [JsonProperty("data")]
    public WebsocketEventV4InnerData? InnerData { get; set; }
}

public class WebsocketEventV4InnerData
{
    [JsonProperty("EventType")]
    public WebsocketEventType? EventType { get; set; }

    [JsonProperty("EventID")]
    public string? EventId { get; set; }

    [JsonProperty("ServerID")]
    public long? ServerId { get; set; }

    [JsonProperty("AllocationID")]
    public string? AllocationId { get; set; }
}

public enum WebsocketEventType
{
    [JsonProperty("AllocateEventType")]
    AllocateEventType,

    [JsonProperty("DeallocateEventType")]
    DeallocateEventType
}
