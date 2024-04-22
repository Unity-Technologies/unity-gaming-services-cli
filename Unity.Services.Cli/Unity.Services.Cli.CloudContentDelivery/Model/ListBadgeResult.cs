using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class ListBadgeResult
{
    public ListBadgeResult(string releaseId, long releaseNum, string name)
    {
        ReleaseId = releaseId;
        ReleaseNum = releaseNum;
        Name = name;
    }

    public string Name { get; set; }
    public string ReleaseId { get; }
    public long ReleaseNum { get; set; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
