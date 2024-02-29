using System.CommandLine;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Input;

class CoreDumpUpdateInput : FleetIdInput
{
    public static readonly Option<string> StorageTypeOption = new(
        new[]
        {
            "--storage-type",
            "-t"
        },
        "The storage type of the core dump");

    public static readonly Option<string> GcsCredentialsFileOption = new(
        new[]
        {
            "--gcs-credentials-file",
            "-c"
        },
        "The path to the JSON file that contains the GCS credentials for the service account");

    public static readonly Option<string> GcsBucketOption = new(
        new[]
        {
            "--gcs-bucket",
            "-b"
        },
        "The name of the GCS bucket to store the core dump");

    public static readonly Option<string> StateOption = new(
        new[]
        {
            "--state",
            "-s"
        },
        "Enable or disable core dump collection");


    static CoreDumpUpdateInput()
    {
        StateOption.FromAmong(
            CoreDumpStateConverter.StateEnum.Enabled.ToString().ToLower(),
            CoreDumpStateConverter.StateEnum.Disabled.ToString().ToLower()
        );
        StorageTypeOption.FromAmong(UpdateCoreDumpConfigRequest.StorageTypeEnum.Gcs.ToString().ToLower());
    }

    [InputBinding(nameof(StorageTypeOption))]
    public string? StorageType { get; init; } = null;

    [InputBinding(nameof(GcsCredentialsFileOption))]
    public string? CredentialsFile { get; init; } = null!;

    [InputBinding(nameof(GcsBucketOption))]
    public string? GcsBucket { get; init; } = null!;

    [InputBinding(nameof(StateOption))]
    public string? State { get; init; } = null;
}
