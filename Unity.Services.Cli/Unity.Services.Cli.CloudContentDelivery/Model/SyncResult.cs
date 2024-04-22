using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class SyncResult
{

    public List<SyncEntry> EntriesToAdd { get; set; } = new();
    public List<SyncEntry> EntriesToUpdate { get; set; } = new();
    public List<SyncEntry> EntriesToDelete { get; set; } = new();
    public List<SyncEntry> EntriesToSkip { get; set; } = new();

    public string GetSummary()
    {
        var totalCount = EntriesToAdd.Count + EntriesToUpdate.Count +
                         EntriesToDelete.Count + EntriesToSkip.Count;
        return $"Total Operation Count: {totalCount}, Entries To Add: {EntriesToAdd.Count}, Entries To Update: {EntriesToUpdate.Count}, Entries To Delete: {EntriesToDelete.Count}, Entries To Skip: {EntriesToSkip.Count}";
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
