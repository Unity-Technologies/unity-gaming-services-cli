using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QueueConfig = Unity.Services.Gateway.MatchmakerAdminApiV3.Generated.Model.QueueConfig;

namespace Unity.Services.Cli.Matchmaker.Model;

class QueueListOutput
{
    public List<QueueSummary> Queues { get; }

    public QueueListOutput(List<QueueConfig> queues)
    {
        Queues = queues.Select(q => new QueueSummary(q)).ToList();
    }

    public override string ToString()
    {
        var jsonString = JsonConvert.SerializeObject(Queues);
        var formattedJson = JToken.Parse(jsonString).ToString(Formatting.Indented);
        return formattedJson;
    }
}

class QueueSummary
{
    public string? Name { get; set; }
    public bool? Enabled { get; set; }
    public int? MaxPlayersPerTicket { get; set; }

    public QueueSummary(QueueConfig queue)
    {
        Name = queue.Name;
        Enabled = queue.Enabled;
        MaxPlayersPerTicket = queue.MaxPlayersPerTicket;
    }
}
