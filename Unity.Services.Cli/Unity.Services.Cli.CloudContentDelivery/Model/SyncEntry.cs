namespace Unity.Services.Cli.CloudContentDelivery.Model;

public class SyncEntry
{
    public SyncEntry(
        string path,
        string environmentId = "",
        string bucketId = "",
        string projectId = "",
        long? contentSize = null,
        string contentType = "",
        string contentHash = "",
        string? entryId = "",
        string? versionId = "",
        List<string>? labels = null,
        object? metadata = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        EnvironmentId = environmentId;
        BucketId = bucketId;
        ProjectId = projectId;
        ContentSize = contentSize!;
        ContentType = contentType;
        ContentHash = contentHash;
        Labels = labels;
        Metadata = metadata;
        EntryId = entryId ?? "";
        VersionId = versionId ?? "";
    }

    public string Path { get; }
    public long? ContentSize { get; }
    public string ContentType { get; }
    public string ContentHash { get; }
    public string EnvironmentId { get; }
    public string BucketId { get; }
    public string ProjectId { get; }
    public List<string>? Labels { get; }
    public object? Metadata { get; }
    public string EntryId { get; set; }
    public string VersionId { get; set; }
}
