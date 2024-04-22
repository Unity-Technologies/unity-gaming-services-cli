namespace Unity.Services.Cli.CloudContentDelivery.Service;

public class ClientWrapper : IClientWrapper
{
    public IReleaseClient? ReleaseClient { get; private set; }
    public IBadgeClient? BadgeClient { get; private set; }
    public IBucketClient? BucketClient { get; private set; }
    public IEntryClient? EntryClient { get; private set; }

    public ClientWrapper(
        IReleaseClient? releaseClient,
        IBadgeClient? badgeClient,
        IBucketClient? bucketClient,
        IEntryClient? entryClient)
    {
        ReleaseClient = releaseClient;
        BadgeClient = badgeClient;
        BucketClient = bucketClient;
        EntryClient = entryClient;
    }

}
