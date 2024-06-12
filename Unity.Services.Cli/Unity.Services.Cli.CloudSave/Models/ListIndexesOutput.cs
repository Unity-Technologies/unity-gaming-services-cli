using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudSave.Models;

public class ListIndexesOutput
{
    public ListIndexesOutput(GetIndexIdsResponse response)
    {
        IndexIds = response.Indexes == null
            ? new List<LiveIndexConfigOutput>()
            : response.Indexes.Select(i => new LiveIndexConfigOutput(i)).ToList();
    }

    public override string ToString()
    {
        var jsonString = JsonConvert.SerializeObject(this);
        var formattedJson = JToken.Parse(jsonString).ToString(Formatting.Indented);
        return formattedJson;
    }

    public List<LiveIndexConfigOutput>? IndexIds { get; }
}

public class LiveIndexConfigOutput
{
    public LiveIndexConfigOutput(LiveIndexConfigInner response)
    {
        Id = response.Id;
        Status = response.Status;
        EntityType = response.EntityType;
        AccessClass = response.AccessClass;
        Fields = response.Fields == null ? new List<IndexFieldOutput>() : response.Fields.Select(f => new IndexFieldOutput(f)).ToList();
    }

    public string Id { get; }

    public IndexStatus? Status { get; }

    public LiveIndexConfigInner.EntityTypeEnum? EntityType { get; }

    public AccessClass? AccessClass { get; }

    public List<IndexFieldOutput>? Fields { get; }
}

public class IndexFieldOutput
{
    public IndexFieldOutput(IndexField response)
    {
        Key = response.Key;
        Ascending = response.Asc;
    }

    public string Key { get; }

    public bool Ascending { get; }
}
