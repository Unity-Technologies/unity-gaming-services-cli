using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

class BuildCreateVersionInput : CommonInput
{
    public const string AccessKeyKey = "--access-key";
    public const string BuildIdKey = "build-id";
    public const string BucketUrlKey = "--bucket-url";
    public const string ContainerTagKey = "--tag";
    public const string FileDirectoryKey = "--directory";
    public const string RemoveOldFilesKey = "--remove-old-files";
    public const string SecretKeyKey = "--secret-key";
    public const string ServiceAccountJsonFileKey = "--service-account-json-file";
    public const string BuildVersionNameKey = "--build-version-name";

    public static readonly Option<string> AccessKeyOption = new(
        AccessKeyKey,
        "The Amazon Web Services (AWS) access key, for s3 bucket builds");

    public static readonly Argument<string> BuildIdArgument = new(
        BuildIdKey,
        "The ID of the build to create a new version for");

    public static readonly Option<string> BucketUrlOption = new(
        BucketUrlKey,
        "The bucket url to use for the build version, for s3 or gcs bucket builds");

    public static readonly Option<string> ContainerTagOption = new(
        ContainerTagKey,
        "The container image tag to use for the build version, for container builds");

    public static readonly Option<string> FileDirectoryOption = new(
        FileDirectoryKey,
        "The directory of files to upload to the build version, for file upload builds");

    public static readonly Option<bool> RemoveOldFilesOption = new(
        RemoveOldFilesKey,
        "Whether to remove old files from the build version, for file upload builds");

    public static readonly Option<string> SecretKeyOption = new(
        SecretKeyKey,
        "The Amazon Web Services (AWS) secret key, for s3 bucket builds");

    public static readonly Option<string> BuildVersionNameOption = new(
        BuildVersionNameKey,
        "The name of the build version to create");

    public static readonly Option<string> ServiceAccountJsonFileOption = new(
        ServiceAccountJsonFileKey,
        "The path to the service account JSON file, for GCS builds");

    [InputBinding(nameof(AccessKeyOption))]
    public string? AccessKey { get; init; }

    [InputBinding(nameof(BuildIdArgument))]
    public string? BuildId { get; init; }

    [InputBinding(nameof(BucketUrlOption))]
    public string? BucketUrl { get; init; }

    [InputBinding(nameof(ContainerTagOption))]
    public string? ContainerTag { get; init; }

    [InputBinding(nameof(FileDirectoryOption))]
    public string? FileDirectory { get; init; }

    [InputBinding(nameof(RemoveOldFilesOption))]
    public bool? RemoveOldFiles { get; init; }

    [InputBinding(nameof(SecretKeyOption))]
    public string? SecretKey { get; init; }

    [InputBinding(nameof(BuildVersionNameOption))]
    public string? BuildVersionName { get; init; }

    [InputBinding(nameof(ServiceAccountJsonFileOption))]
    public string? ServiceAccountJsonFile { get; init; }
}
