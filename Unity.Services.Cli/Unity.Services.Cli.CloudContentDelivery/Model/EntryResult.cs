using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class EntryResult
{
    public EntryResult(CcdCreateOrUpdateEntryBatch200ResponseInner? response)
    {
        if (response == null)
            throw new CliException(
                "A server error occurred while retrieving the entry result. Please try again later.",
                ExitCode.HandledError);

        Entryid = response.Entryid;
        CurrentVersionid = response.CurrentVersionid;
        ContentHash = response.ContentHash;
        Complete = response.Complete;
        Labels = response.Labels;
        Link = response.Link;
        ContentSize = response.ContentSize;
        ContentType = response.ContentType;
        ContentHash = response.ContentHash;
        ContentLink = response.ContentLink;
        LastModified = response.LastModified;
        Path = response.Path;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (response.Metadata != null)
            Metadata = response.Metadata.ToString();
    }

    public Guid Entryid { get; set; }

    public string? Path { get; set; }

    public Guid CurrentVersionid { get; set; }

    public bool Complete { get; set; }

    public string ContentType { get; set; }

    public long ContentSize { get; set; }

    public string ContentHash { get; set; }

    public string? ContentLink { get; set; }

    public List<string> Labels { get; set; }

    public DateTime LastModified { get; set; }

    public string Link { get; set; }

    public string? Metadata { get; set; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
