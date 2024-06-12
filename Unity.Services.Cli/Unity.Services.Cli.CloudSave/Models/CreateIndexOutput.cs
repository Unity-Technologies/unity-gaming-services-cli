using Newtonsoft.Json;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudSave.Models;

public class CreateIndexOutput
{
    public IndexStatus Status { get; set; }
    public string Id { get; set; }

    public CreateIndexOutput(CreateIndexResponse response)
    {
        Status = response.Status;
        Id = response.Id;
    }

    public override string ToString()
    {
        return $"Index with ID \"{Id}\" successfully created with status \"{Status}\".";
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}
