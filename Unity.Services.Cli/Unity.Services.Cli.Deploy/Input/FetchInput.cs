using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Deploy.Input;

/// <summary>
/// Fetch command input
/// </summary>
public class FetchInput : CommonInput
{
    public static readonly Argument<string> PathArgument = new(
        "path",
        $"The path to fetch to. Accepts a directory path to fetch to. Specify '.' to fetch in current directory");

    [InputBinding(nameof(PathArgument))]
    public string Path { get; set; } = string.Empty;

    public static readonly Option<bool> ReconcileOption = new(new[]
    {
        "--reconcile"
    }, "Content that is not updated will be created at the root.");

    [InputBinding(nameof(ReconcileOption))]
    public bool Reconcile { get; set; } = false;

    public static readonly Option<bool> DryRunOption = new(new[]
    {
        "--dry-run"
    }, "Perform a trial run with no changes made.");

    [InputBinding(nameof(DryRunOption))]
    public bool DryRun { get; set; } = false;

}
