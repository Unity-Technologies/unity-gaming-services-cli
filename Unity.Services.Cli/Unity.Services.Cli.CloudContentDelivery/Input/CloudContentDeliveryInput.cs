using System.CommandLine;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.SystemEnvironment;

namespace Unity.Services.Cli.CloudContentDelivery.Input;

public class CloudContentDeliveryInput : CommonInput
{

    public static readonly Argument<string> AddressArgument = new(
        "address",
        "The address to send GET request");

    public static readonly Argument<string> LocalFolderArgument = new(
        "local-directory-path",
        "The local directory path to synchronize");

    public static readonly Argument<string> EntryPathArgument = new(
        "entry-path",
        "The path of the entry");

    public static readonly Option<string> OutputFileOption = new(
        new[]
        {
            "-o",
            "--output"
        },
        "Write output to file instead of stdout");

    public static readonly Option<bool> DeleteOption = new(
        new[]
        {
            "-x",
            "--delete"
        },
        "Entries that do not exist locally are deleted during sync.");

    public static readonly Option<string> ExclusionPatternOption = new(
        new[]
        {
            "-f",
            "--exclude"
        },
        "Exclude files and folders matching this pattern.");

    public static readonly Option<bool> CreateReleaseOption = new(
        new[]
        {
            "-r",
            "--create-release"
        },
        "Creates a release containing the files that were synced.");

    public static readonly Option<bool> VerboseOption = new(
        new[]
        {
            "-v",
            "--verbose"
        },
        "Turn on verbose mode for more synchronization details.");

    public static readonly Option<string> UpdateBadgeOption = new(
        new[]
        {
            "-u",
            "--badge"
        },
        "If release flag is set, badge to be assigned to the release.");

    public static readonly Option<string> ReleaseMetadataOption = new(
        new[]
        {
            "-m",
            "--release-metadata"
        },
        "JSON metadata associated with the release.");

    public static readonly Option<string> SyncMetadataOption = new(
        new[]
        {
            "-m",
            "--metadata"
        },
        "JSON metadata associated with the entries and release.");

    public static readonly Option<string> ReleaseNotesOption = new(
        new[]
        {
            "-n",
            "--release-notes"
        },
        "If release flag is set, notes associated with the release.");

    public static readonly Option<bool> IncludeSyncEntriesOnlyOption = new(
        new[]
        {
            "-i",
            "--include-entries-added-during-sync"
        },
        "Including entries added to the bucket during the ongoing synchronization process to ensure all new additions are captured in the release.");

    public static readonly Option<bool> DryRunOption = new(
        new[]
        {
            "-d",
            "--dry-run"
        },
        "Prints the operations that would be performed without actually executing them.");

    public static readonly Option<int?> RetryOption = new(
        new[]
        {
            "-z",
            "--retry"
        },
        "Number of times to retry syncing a file (default is 3).");

    public static readonly Option<int> TimeoutOption = new(
        new[]
        {
            "-t",
            "--timeout"
        },
        "Upload timeout in seconds (default is no timeout).");

    public static readonly Argument<string> LocalPathArgument = new("local-path", "Local file path.");

    public static readonly Argument<string> RemotePathArgument = new("remote-path", "Remote file path.");

    public static readonly Argument<string> BadgeNameArgument = new("badge-name", "Name of the badge.");

    public static readonly Argument<string> BucketNameArgument = new("bucket-name", "Name of the bucket.");

    public static readonly Argument<int> ReleaseNumArgument = new("release-num", "Release number.");

    public static readonly Argument<string> EntryIdArgument = new("entry-id", "Entry id.");

    public static readonly Option<string> BucketNameOption = new(
        new[]
        {
            "-b",
            "--bucket-name"
        },
        "Name of the bucket.");

    public static readonly Option<string> MetadataOption = new(
        new[]
        {
            "-m",
            "--metadata"
        },
        "JSON metadata associated with this entry.");

    public static readonly Option<int> PageOption = new(
        new[]
        {
            "-pa",
            "--page"
        },
        "Current Page.");

    public static readonly Option<int> PerPageOption = new(
        new[]
        {
            "-pp",
            "--per-page"
        },
        "Items Per Page.");

    public static readonly Option<string> FilterNameOption = new(
        new[]
        {
            "-n",
            "--filter-by-name"
        },
        "Filter by name.");

    public static readonly Option<string> SortOrderOption = new(
        new[]
        {
            "-o",
            "--sort-order"
        },
        "Sort order (asc, desc).");

    public static readonly Option<string> SortByOption = new(
        new[]
        {
            "-s",
            "--sort-by"
        },
        "Sort by values.");

    public static readonly Option<string> SortByBadgeOption = new(
        new[]
        {
            "-s",
            "--sort-by"
        },
        "Sort by values (name, releasenum, created).");

    public static readonly Option<string> SortByReleaseOption = new(
        new[]
        {
            "-s",
            "--sort-by"
        },
        "Sort by values (releasenum, created).");

    public static readonly Option<string> SortByEntryOption = new(
        new[]
        {
            "-s",
            "--sort-by"
        },
        "Sort by values (path, content_size, content_type, last_modified).");

    public static readonly Option<string> ReleaseNumOpt = new(
        new[]
        {
            "-r",
            "--release-number"
        },
        "Release number.");

    public static readonly Option<string> BadgeOption = new(
        new[]
        {
            "-u",
            "--badges"
        },
        "Badges associated with the release.");

    public static readonly Option<string> StartingAfterOption = new(
        new[]
        {
            "-a",
            "--starting-after"
        },
        "Returns entries listed after the named entry id");

    public static readonly Option<string> PathOption = new(
        new[]
        {
            "-t",
            "--path"
        },
        "The path of the entry.");

    public static readonly Option<string> LabelOption = new(
        new[]
        {
            "-l",
            "--label"
        },
        "The label of the entry.");

    public static readonly Option<List<string>> LabelsOption = new(
        new[]
        {
            "-l",
            "--labels"
        },
        "List of labels.");

    public static readonly Option<string> ContentTypeOption = new(
        new[]
        {
            "-c",
            "--content-type"
        },
        "The content type of the entry.");

    public static readonly Option<bool> CompleteOption = new(
        new[]
        {
            "-w",
            "--complete"
        },
        "Is the content is uploaded or not.");

    public static readonly Option<string> PromotedFromReleaseOption = new(
        new[]
        {
            "-pr",
            "--promoted-from-release"
        },
        "The release where the promotion originated.");

    public static readonly Option<string> PromotedFromBucketOption = new(
        new[]
        {
            "-pb",
            "--promoted-from-bucket"
        },
        "The bucket from which the release was promoted.");

    public static readonly Option<string> NoteOption = new(
        new[]
        {
            "-n",
            "--notes"
        },
        "Notes associated with the release.");

    public static readonly Option<string> VersionIdOption = new(
        new[]
        {
            "-v",
            "--version-id"
        },
        "Version id of the entry.");

    public static readonly Option<string> NoteOptionRequired = new(
        new[]
        {
            "-n",
            "--notes"
        },
        "Notes associated with the release.")
    {
        IsRequired = true
    };

    public static readonly Argument<string> TargetEnvironmentNameArgument = new(
        "target-environment-name",
        "Name of the target environment.");

    public static readonly Argument<string> PromotionIdArgument = new("promotion-id", "Promotion id.");

    public static readonly Argument<string> ReleaseIdArgument = new("release-id", "Release id.");

    public static readonly Argument<string> TargetBucketNameArgument = new(
        "target-bucket-name",
        "Name of the target bucket.");

    [InputBinding(nameof(TargetEnvironmentNameArgument))]
    public string? TargetEnvironment { get; set; }

    [InputBinding(nameof(PromotionIdArgument))]
    public string? PromotionId { get; set; }

    [InputBinding(nameof(TargetBucketNameArgument))]
    public string? TargetBucketName { get; set; }

    [EnvironmentBinding(Keys.EnvironmentKeys.BucketName)]
    [ConfigBinding(Keys.ConfigKeys.BucketName)]
    [InputBinding(nameof(BucketNameOption))]
    public string? BucketNameOpt { get; set; }

    [InputBinding(nameof(AddressArgument))]
    public string? Address { get; set; }

    [InputBinding(nameof(EntryPathArgument))]
    public string? EntryPath { get; set; }

    [InputBinding(nameof(LocalFolderArgument))]
    public string? LocalFolder { get; set; }

    [InputBinding(nameof(OutputFileOption))]
    public string? OutputFile { get; set; }

    [InputBinding(nameof(DeleteOption))]
    public bool? Delete { get; set; }

    [InputBinding(nameof(ExclusionPatternOption))]
    public string? ExclusionPattern { get; set; }

    [InputBinding(nameof(DryRunOption))]
    public bool? DryRun { get; set; }

    [InputBinding(nameof(IncludeSyncEntriesOnlyOption))]
    public bool? IncludeSyncEntriesOnly { get; set; }

    [InputBinding(nameof(CreateReleaseOption))]
    public bool? CreateRelease { get; set; }

    [InputBinding(nameof(UpdateBadgeOption))]
    public string? UpdateBadge { get; set; }

    [InputBinding(nameof(VerboseOption))]
    public bool? Verbose { get; set; }

    [InputBinding(nameof(ReleaseMetadataOption))]
    public string? ReleaseMetadata { get; set; }

    [InputBinding(nameof(SyncMetadataOption))]
    public string? SyncMetadata { get; set; }

    [InputBinding(nameof(ReleaseNotesOption))]
    public string? ReleaseNotes { get; set; }

    [InputBinding(nameof(RetryOption))]
    public int? Retry { get; set; }

    [InputBinding(nameof(TimeoutOption))]
    public double Timeout { get; set; }

    [InputBinding(nameof(LocalPathArgument))]
    public string? LocalPath { get; set; }

    [InputBinding(nameof(RemotePathArgument))]
    public string? RemotePath { get; set; }

    [InputBinding(nameof(BadgeNameArgument))]
    public string? BadgeName { get; set; }

    [InputBinding(nameof(BucketNameArgument))]
    public string? BucketName { get; set; }

    [InputBinding(nameof(EntryIdArgument))]
    public string? EntryId { get; set; }

    [InputBinding(nameof(VersionIdOption))]
    public string? VersionId { get; set; }

    [InputBinding(nameof(MetadataOption))]
    public string? Metadata { get; set; }

    [InputBinding(nameof(PageOption))]
    public int? Page { get; set; }

    [InputBinding(nameof(PerPageOption))]
    public int? PerPage { get; set; }

    [InputBinding(nameof(FilterNameOption))]
    public string? FilterName { get; set; }

    [InputBinding(nameof(SortOrderOption))]
    public string? SortOrder { get; set; }

    [InputBinding(nameof(SortByOption))]
    public string? SortBy { get; set; }

    [InputBinding(nameof(SortByBadgeOption))]
    public string? SortByBadge { get; set; }

    [InputBinding(nameof(SortByReleaseOption))]
    public string? SortByRelease { get; set; }

    [InputBinding(nameof(SortByEntryOption))]
    public string? SortByEntry { get; set; }

    [InputBinding(nameof(ReleaseNumOpt))]
    public string? ReleaseNumOption { get; set; }

    [InputBinding(nameof(BadgeOption))]
    public string? Badges { get; set; }

    [InputBinding(nameof(StartingAfterOption))]
    public string? StartingAfter { get; set; }

    [InputBinding(nameof(PathOption))]
    public string? Path { get; set; }

    [InputBinding(nameof(LabelOption))]
    public string? Label { get; set; }

    [InputBinding(nameof(LabelsOption))]
    public List<string>? Labels { get; set; }

    [InputBinding(nameof(ContentTypeOption))]
    public string? ContentType { get; set; }

    [InputBinding(nameof(CompleteOption))]
    public bool? Complete { get; set; }

    [InputBinding(nameof(NoteOption))]
    public string? Notes { get; set; }

    [InputBinding(nameof(NoteOptionRequired))]
    public string? NotesRequired { get; set; }

    [InputBinding(nameof(ReleaseIdArgument))]
    public string? ReleaseId { get; set; }

    [InputBinding(nameof(PromotedFromReleaseOption))]
    public string? PromotedFromRelease { get; set; }

    [InputBinding(nameof(PromotedFromBucketOption))]
    public string? PromotedFromBucket { get; set; }

    [InputBinding(nameof(ReleaseNumArgument))]
    public int? ReleaseNum { get; set; }

}
