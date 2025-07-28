using Newtonsoft.Json;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudSave.Models;

public class GetDataItemsOutput
{
    public List<Item>? Items { get; set; }
    public string? Next { get; set; }

    public GetDataItemsOutput(GetItemsResponse response)
    {
        Items = response.Results;
        Next = response.Links.Next;
    }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
