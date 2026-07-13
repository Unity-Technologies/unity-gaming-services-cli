using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Gateway.ObservabilityApiV1.Generated.Model;

namespace Unity.Services.Cli.Observability.Model;

class GetLogsResponseOutput
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public List<LogRecordOutput> Results { get; set; }

    public GetLogsResponseOutput(LogsResponse response)
    {
        Offset = response.Offset;
        Limit = response.Limit;
        Total = response.Total;
        Results = (response.Results ?? new List<LogRecord>())
            .Select(r => new LogRecordOutput(r))
            .ToList();
    }

    public override string ToString()
    {
        var jsonString = JsonConvert.SerializeObject(this);
        return JToken.Parse(jsonString).ToString(Formatting.Indented);
    }
}

class LogRecordOutput
{
    public string Timestamp { get; set; }
    public string SeverityText { get; set; }
    public int SeverityNumber { get; set; }
    public string Body { get; set; }
    public Dictionary<string, object> ResourceAttributes { get; set; }
    public Dictionary<string, object> LogAttributes { get; set; }

    public LogRecordOutput(LogRecord record)
    {
        Timestamp = record.Timestamp;
        SeverityText = record.SeverityText;
        SeverityNumber = record.SeverityNumber;
        Body = record.Body;
        ResourceAttributes = record.ResourceAttributes ?? new Dictionary<string, object>();
        LogAttributes = record.LogAttributes ?? new Dictionary<string, object>();
    }
}
