using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class ReleaseResult
{
    public ReleaseResult(
        CcdGetBucket200ResponseLastRelease? response
    )
    {
        if (response == null)
            throw new CliException(
                "A server error occurred while retrieving the release result. Please try again later.",
                ExitCode.HandledError);

        Notes = response.Notes;
        Badges = response.Badges;
        ReleaseId = response.Releaseid;
        ReleaseNum = response.Releasenum;
        PromotedFromBucket = response.PromotedFromBucket;
        PromotedFromRelease = response.PromotedFromRelease;
        Created = response.Created;
        Changes = response.Changes;
        ContentHash = response.ContentHash;
        ContentSize = response.ContentSize;
        EntriesLink = response.EntriesLink;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (response.Metadata != null)
            Metadata = JsonConvert.SerializeObject(response.Metadata, Formatting.Indented);
    }

    public Guid ReleaseId { get; set; }

    public long ReleaseNum { get; set; }

    public long ContentSize { get; set; }

    public string? ContentHash { get; set; }

    public CcdGetBucket200ResponseChanges? Changes { get; set; }

    public List<CcdGetBucket200ResponseLastReleaseBadgesInner>? Badges { get; set; }

    public DateTime Created { get; set; }

    public string? EntriesLink { get; set; }

    public string? Metadata { get; set; }

    public string? Notes { get; set; }

    public Guid PromotedFromBucket { get; set; }

    public Guid PromotedFromRelease { get; set; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
