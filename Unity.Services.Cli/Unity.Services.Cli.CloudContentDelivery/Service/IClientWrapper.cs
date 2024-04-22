namespace Unity.Services.Cli.CloudContentDelivery.Service;

interface IClientWrapper
{
    IReleaseClient? ReleaseClient { get; }
    IBadgeClient? BadgeClient { get; }
    IBucketClient? BucketClient { get; }
    IEntryClient? EntryClient { get; }
}
