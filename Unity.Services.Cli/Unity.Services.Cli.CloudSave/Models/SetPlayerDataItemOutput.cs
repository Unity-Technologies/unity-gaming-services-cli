using Newtonsoft.Json;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudSave.Models;

public class SetPlayerDataItemOutput
{
    public string? WriteLock { get; set; }

    public SetPlayerDataItemOutput(SetItemResponse response)
    {
        WriteLock = response?.WriteLock;
    }

    public override string ToString()
    {
        if (WriteLock != null)
        {
            return $"Item successfully written with writelock \"{WriteLock}\".";
        }
        return "Item successfully written.";
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}
