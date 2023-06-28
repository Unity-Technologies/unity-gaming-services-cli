using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Authoring.Input;

/// <summary>
/// Deploy command input
/// </summary>
public class DeployInput : CommonInput
{
    public static readonly Argument<ICollection<string>> PathsArgument = new(
        "paths",
        "The paths to deploy from. Accepts multiple directory or file paths. Specify '.' to deploy in current directory");

    [InputBinding(nameof(PathsArgument))]
    public IReadOnlyList<string> Paths { get; set; } = new List<string>();

    public static readonly Option<bool> DryRunOption = new(
        new[]
        {
            "--dry-run"
        },
        "Perform a trial run with no changes made.");

    [InputBinding(nameof(DryRunOption))]
    public bool DryRun { get; set; } = false;

    public static readonly Option<bool> ReconcileOption = new(
        new[]
        {
            "--reconcile"
        },
        "Delete content not part of deploy.");

    [InputBinding(nameof(ReconcileOption))]
    public bool Reconcile { get; set; } = false;

    public static readonly Option<ICollection<string>> ServiceOptions = new(
        new[]
        {
            "--services",
            "-s"
        },
        "The name(s) of the service(s) to perform the command on.");

    [InputBinding(nameof(ServiceOptions))]
    public IReadOnlyList<string> Services { get; set; } = new List<string>();
}
