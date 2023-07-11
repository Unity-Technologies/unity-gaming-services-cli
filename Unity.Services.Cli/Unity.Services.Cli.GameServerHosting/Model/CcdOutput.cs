using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class CcdOutput
{
    public CcdOutput(CCDDetails? ccdDetails)
    {
        if (ccdDetails == null) return;
        BucketId = ccdDetails.BucketID;
        ReleaseId = ccdDetails.ReleaseID;
    }

    public Guid BucketId { get; }

    public Guid ReleaseId { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
