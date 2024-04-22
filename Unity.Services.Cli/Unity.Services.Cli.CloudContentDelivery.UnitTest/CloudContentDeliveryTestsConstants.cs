namespace CloudContentDeliveryTest;

public static class CloudContentDeliveryTestsConstants
{
    public const string ProjectId = "00000000-0000-0000-0000-000000000001";
    public const string BucketId = "00000000-0000-0000-0000-000000000002";
    public const string BucketName = "NewBucket";
    public const string TargetBucketName = "TargetBucket";


    public const string BadgeName = "NewBadge";
    public const string BucketDescription = "Description";
    public const string EnvironmentId = "00000000-0000-0000-0000-000000000003";
    public const string ReleaseId = "00000000-0000-0000-0000-000000000004";
    public const string EntryId = "00000000-0000-0000-0000-000000000005";
    public const string PromotionId = "00000000-0000-0000-0000-000000000006";
    public const string PromotedFromRelease = "00000000-0000-0000-0000-000000000007";
    public const string PromotedFromBucket = "00000000-0000-0000-0000-000000000008";

    public const string ToBucket = "00000000-0000-0000-0000-000000000009";
    public const string ToEnvironment = "production";

    public const int ReleaseNumber = 1;
    public const string Notes = "my notes";
    public const string Metadata = "";
    public const bool PromoteOnly = true;
    public const int Page = 1;
    public const int PerPage = 10;

    public static readonly string StartingAfter = "00000000-0000-0000-0000-000000000011";
    public const string Path = "folder/file.jpg";

    public static readonly string VersionId = "00000000-0000-0000-0000-000000000011";
    public const string? Label = "abc,def";
    public const string FilterName = "";

    public static readonly List<string> Labels = new()
    {
        "abc",
        "def"
    };

    public const string LocalFolder = "folder";
    public const string ExclusionPattern = "";
    public const bool Delete = true;
    public const int Retry = 3;
    public const bool DryRun = false;
    public const string UpdateBadge = "mybadge";
    public const bool CreateRelease = true;

    public const string ContentType = "image/jpeg";
    public const bool Complete = true;
    public const string SortBy = "name";
    public const string SortOrder = "desc";

}
