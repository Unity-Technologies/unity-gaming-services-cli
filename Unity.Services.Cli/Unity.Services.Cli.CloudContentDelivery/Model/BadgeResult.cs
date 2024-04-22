using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class BadgeResult
{
    public BadgeResult(
        CcdGetBucket200ResponseLastReleaseBadgesInner? response
    )
    {
        if (response == null)
            throw new CliException(
                "A server error occurred while retrieving the badge result. Please try again later.",
                ExitCode.HandledError);

        Name = response.Name;
        ReleaseId = response.Releaseid;
        ReleaseNum = response.Releasenum;
        Created = response.Created;
    }

    public string? Name { get; set; }
    public Guid ReleaseId { get; set; }
    public long ReleaseNum { get; set; }
    public DateTime Created { get; set; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
