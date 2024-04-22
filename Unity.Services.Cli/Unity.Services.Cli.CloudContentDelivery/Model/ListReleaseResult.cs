using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class ListReleaseResult
{
    public ListReleaseResult(
        string releaseId,
        long releaseNum)
    {
        ReleaseId = releaseId;
        ReleaseNum = releaseNum;
    }

    public string ReleaseId { get; set; }
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
